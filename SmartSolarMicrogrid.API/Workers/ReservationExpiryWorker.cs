using SmartSolarMicrogrid.API.Data;
using SmartSolarMicrogrid.API.Repositories.Interfaces;

namespace SmartSolarMicrogrid.API.Workers;

public class ReservationExpiryWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReservationExpiryWorker> _logger;

    public ReservationExpiryWorker(IServiceProvider serviceProvider, ILogger<ReservationExpiryWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReservationExpiryWorker initialized. Checking every 5 minutes.");

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessExpiriesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during reservation expiry worker cycle.");
            }
        }
    }

    private async Task ProcessExpiriesAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IReservationRepository>();
        var slotRepo = scope.ServiceProvider.GetRequiredService<ISlotRepository>();
        var tx = scope.ServiceProvider.GetRequiredService<ITransactionRunner>();

        var nowUtc = DateTime.UtcNow;

        // 1. Pending reservations whose slot start time has passed -> Expired
        var pendingToExpire = await repo.GetPendingPastStartUtcAsync(nowUtc, stoppingToken);
        foreach (var r in pendingToExpire)
        {
            try
            {
                r.Expire(nowUtc);
                // Release held bay and save
                await tx.RunAsync(async (session, token) =>
                {
                    await slotRepo.ReleaseBayAsync(session, r.SlotId, token);
                    await repo.SaveInTransactionAsync(session, r, token);
                }, stoppingToken);

                _logger.LogInformation("Reservation {ReservationNo} expired (slot started).", r.ReservationNo);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to expire reservation {ReservationNo}: {Message}", r.ReservationNo, ex.Message);
            }
        }

        // 2. Approved reservations whose slot end time has passed -> NoShow
        var approvedToNoShow = await repo.GetApprovedPastEndUtcAsync(nowUtc, stoppingToken);
        foreach (var r in approvedToNoShow)
        {
            try
            {
                r.MarkNoShow(nowUtc);
                await repo.SaveAsync(r, stoppingToken);
                _logger.LogInformation("Reservation {ReservationNo} marked as NoShow (slot ended without transfer).", r.ReservationNo);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to mark NoShow for reservation {ReservationNo}: {Message}", r.ReservationNo, ex.Message);
            }
        }
    }
}
