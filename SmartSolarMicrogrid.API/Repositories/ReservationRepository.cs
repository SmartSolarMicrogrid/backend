using MongoDB.Bson;
using MongoDB.Driver;
using SmartSolarMicrogrid.API.Common.Errors;
using SmartSolarMicrogrid.API.Data;
using SmartSolarMicrogrid.API.Models;
using SmartSolarMicrogrid.API.Repositories.Interfaces;

namespace SmartSolarMicrogrid.API.Repositories;

public class ReservationRepository : IReservationRepository
{
    private readonly MongoDbContext _context;

    public ReservationRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<EnergyReservation?> GetByIdAsync(ObjectId id, CancellationToken ct = default)
    {
        return await _context.Reservations.Find(r => r.Id == id).FirstOrDefaultAsync(ct);
    }

    public async Task<EnergyReservation?> GetByReservationNoAsync(string reservationNo, CancellationToken ct = default)
    {
        return await _context.Reservations.Find(r => r.ReservationNo == reservationNo).FirstOrDefaultAsync(ct);
    }

    public async Task<List<EnergyReservation>> GetByProsumerAsync(string prosumerNic, CancellationToken ct = default)
    {
        return await _context.Reservations
            .Find(r => r.ProsumerNic == prosumerNic)
            .SortByDescending(r => r.SlotStartUtc)
            .ToListAsync(ct);
    }

    public async Task<List<EnergyReservation>> SearchAsync(string? status = null, ObjectId? nodeId = null, List<ObjectId>? allowedNodeIds = null, DateTime? fromUtc = null, DateTime? toUtc = null, int limit = 50, CancellationToken ct = default)
    {
        var builder = Builders<EnergyReservation>.Filter;
        var filter = builder.Empty;

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<ReservationStatus>(status, true, out var parsedStatus))
        {
            filter &= builder.Eq(r => r.Status, parsedStatus);
        }

        if (nodeId.HasValue)
        {
            filter &= builder.Eq(r => r.NodeId, nodeId.Value);
        }
        else if (allowedNodeIds != null && allowedNodeIds.Count > 0)
        {
            filter &= builder.In(r => r.NodeId, allowedNodeIds);
        }

        if (fromUtc.HasValue)
        {
            filter &= builder.Gte(r => r.SlotStartUtc, fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            filter &= builder.Lte(r => r.SlotStartUtc, toUtc.Value);
        }

        return await _context.Reservations
            .Find(filter)
            .SortByDescending(r => r.SlotStartUtc)
            .Limit(limit)
            .ToListAsync(ct);
    }

    public async Task InsertAsync(IClientSessionHandle session, EnergyReservation reservation, CancellationToken ct = default)
    {
        await _context.Reservations.InsertOneAsync(session, reservation, cancellationToken: ct);
    }

    public async Task SaveAsync(EnergyReservation reservation, CancellationToken ct = default)
    {
        var expectedVersion = reservation.Version;
        reservation.Version++;

        var result = await _context.Reservations.ReplaceOneAsync(
            r => r.Id == reservation.Id && r.Version == expectedVersion,
            reservation,
            cancellationToken: ct);

        if (result.MatchedCount == 0)
            throw new DomainException(ErrorCodes.ConcurrentUpdate, "Record was updated concurrently by another operation. Please reload and retry.");
    }

    public async Task SaveInTransactionAsync(IClientSessionHandle session, EnergyReservation reservation, CancellationToken ct = default)
    {
        var expectedVersion = reservation.Version;
        reservation.Version++;

        var result = await _context.Reservations.ReplaceOneAsync(
            session,
            r => r.Id == reservation.Id && r.Version == expectedVersion,
            reservation,
            cancellationToken: ct);

        if (result.MatchedCount == 0)
            throw new DomainException(ErrorCodes.ConcurrentUpdate, "Record was updated concurrently by another operation in transaction.");
    }

    public async Task<long> CountActiveByProsumerAsync(string prosumerNic, CancellationToken ct = default)
    {
        return await _context.Reservations.CountDocumentsAsync(
            r => r.ProsumerNic == prosumerNic && r.IsActive && r.SlotEndUtc > DateTime.UtcNow,
            cancellationToken: ct);
    }

    public async Task<long> CountActiveByNodeAsync(ObjectId nodeId, CancellationToken ct = default)
    {
        return await _context.Reservations.CountDocumentsAsync(
            r => r.NodeId == nodeId && r.IsActive && r.SlotEndUtc > DateTime.UtcNow,
            cancellationToken: ct);
    }

    public async Task<List<EnergyReservation>> GetPendingPastStartUtcAsync(DateTime nowUtc, CancellationToken ct = default)
    {
        return await _context.Reservations.Find(
            r => r.Status == ReservationStatus.Pending && r.SlotStartUtc <= nowUtc).ToListAsync(ct);
    }

    public async Task<List<EnergyReservation>> GetApprovedPastEndUtcAsync(DateTime nowUtc, CancellationToken ct = default)
    {
        return await _context.Reservations.Find(
            r => r.Status == ReservationStatus.Approved && r.SlotEndUtc <= nowUtc).ToListAsync(ct);
    }
}
