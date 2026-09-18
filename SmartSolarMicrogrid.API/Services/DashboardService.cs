using MongoDB.Bson;
using MongoDB.Driver;
using SmartSolarMicrogrid.API.Data;
using SmartSolarMicrogrid.API.DTOs.Dashboard;
using SmartSolarMicrogrid.API.Models;
using SmartSolarMicrogrid.API.Services.Interfaces;
using SmartSolarMicrogrid.API.Utilities;

namespace SmartSolarMicrogrid.API.Services;

public class DashboardService : IDashboardService
{
    private readonly MongoDbContext _context;

    public DashboardService(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<OperatorDashboardDto> GetOperatorDashboardAsync(List<string> operatorNodeIds, CancellationToken ct = default)
    {
        var nodeObjectIds = operatorNodeIds
            .Where(id => ObjectId.TryParse(id, out _))
            .Select(ObjectId.Parse)
            .ToList();

        var todayLocal = DateOnly.FromDateTime(ColomboTime.NowLocal()).ToString("yyyy-MM-dd");
        var todayStartUtc = ColomboTime.ToUtc(DateOnly.FromDateTime(ColomboTime.NowLocal()), new TimeOnly(0, 0));

        var activeSlotsCount = await _context.Slots.CountDocumentsAsync(
            s => nodeObjectIds.Contains(s.NodeId) && s.LocalDate == todayLocal && s.Status != SlotStatus.Blocked,
            cancellationToken: ct);

        var pendingApprovals = await _context.Reservations.CountDocumentsAsync(
            r => nodeObjectIds.Contains(r.NodeId) && r.Status == ReservationStatus.Pending,
            cancellationToken: ct);

        var inProgressTransfers = await _context.Reservations.CountDocumentsAsync(
            r => nodeObjectIds.Contains(r.NodeId) && r.Status == ReservationStatus.InProgress,
            cancellationToken: ct);

        var completedTodayList = await _context.Reservations.Find(
            r => nodeObjectIds.Contains(r.NodeId) && r.Status == ReservationStatus.Completed && r.SlotStartUtc >= todayStartUtc)
            .ToListAsync(ct);

        var todayEnergy = completedTodayList.Sum(r => r.Transaction?.ActualKwh ?? 0m);

        return new OperatorDashboardDto
        {
            AssignedNodeIds = operatorNodeIds,
            ActiveSlotsToday = (int)activeSlotsCount,
            PendingApprovalsCount = (int)pendingApprovals,
            InProgressTransfersCount = (int)inProgressTransfers,
            CompletedTodayCount = completedTodayList.Count,
            TodayEnergyTransferredKwh = todayEnergy
        };
    }

    public async Task<BackofficeDashboardDto> GetBackofficeDashboardAsync(CancellationToken ct = default)
    {
        var totalNodes = await _context.Nodes.CountDocumentsAsync(Builders<SolarStationInfo>.Filter.Empty, cancellationToken: ct);
        var activeNodes = await _context.Nodes.CountDocumentsAsync(n => n.Status == NodeStatus.Active, cancellationToken: ct);

        var totalProsumers = await _context.Prosumers.CountDocumentsAsync(Builders<Prosumer>.Filter.Empty, cancellationToken: ct);
        var activeProsumers = await _context.Prosumers.CountDocumentsAsync(p => p.Status == ProsumerStatus.Active, cancellationToken: ct);

        var activeReservations = await _context.Reservations.CountDocumentsAsync(r => r.IsActive, cancellationToken: ct);

        var completed = await _context.Reservations.Find(r => r.Status == ReservationStatus.Completed).ToListAsync(ct);
        var totalKwh = completed.Sum(r => r.Transaction?.ActualKwh ?? 0m);
        var totalTurnover = completed.Sum(r => r.Transaction?.Value ?? 0m);

        return new BackofficeDashboardDto
        {
            TotalNodesCount = (int)totalNodes,
            ActiveNodesCount = (int)activeNodes,
            TotalProsumersCount = (int)totalProsumers,
            ActiveProsumersCount = (int)activeProsumers,
            ActiveReservationsCount = (int)activeReservations,
            TotalEnergyTradedKwh = totalKwh,
            TotalFinancialTurnover = totalTurnover
        };
    }
}
