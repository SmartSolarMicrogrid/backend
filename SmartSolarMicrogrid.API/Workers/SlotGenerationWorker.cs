using SmartSolarMicrogrid.API.Services.Interfaces;
using SmartSolarMicrogrid.API.Utilities;

namespace SmartSolarMicrogrid.API.Workers;

public class SlotGenerationWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SlotGenerationWorker> _logger;

    public SlotGenerationWorker(IServiceProvider serviceProvider, ILogger<SlotGenerationWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SlotGenerationWorker starting: running initial generation for next 7 days.");
        try
        {
            await RunGenerationAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Initial slot generation run failed.");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var nowColombo = ColomboTime.NowLocal();
                var nextRun = nowColombo.Date.AddDays(1).AddHours(0).AddMinutes(30); // 00:30 AM tomorrow
                var delay = nextRun - nowColombo;

                if (delay <= TimeSpan.Zero)
                    delay = TimeSpan.FromHours(24);

                _logger.LogInformation("SlotGenerationWorker scheduled next run in {Hours:F1} hours at {Time}", delay.TotalHours, nextRun);
                await Task.Delay(delay, stoppingToken);

                await RunGenerationAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while executing SlotGenerationWorker.");
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
        }
    }

    private async Task RunGenerationAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var slotService = scope.ServiceProvider.GetRequiredService<ISlotService>();
            var count = await slotService.GenerateDailySlotsForAllActiveNodesAsync(forwardDays: 7, stoppingToken);
            _logger.LogInformation("SlotGenerationWorker successfully generated/upserted {Count} slots.", count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run slot generation pass.");
        }
    }
}
