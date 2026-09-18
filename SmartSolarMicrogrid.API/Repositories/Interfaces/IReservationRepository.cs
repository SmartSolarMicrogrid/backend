using MongoDB.Bson;
using MongoDB.Driver;
using SmartSolarMicrogrid.API.Models;

namespace SmartSolarMicrogrid.API.Repositories.Interfaces;

public interface IReservationRepository
{
    Task<EnergyReservation?> GetByIdAsync(ObjectId id, CancellationToken ct = default);
    Task<EnergyReservation?> GetByReservationNoAsync(string reservationNo, CancellationToken ct = default);
    Task<List<EnergyReservation>> GetByProsumerAsync(string prosumerNic, CancellationToken ct = default);
    Task<List<EnergyReservation>> SearchAsync(string? status = null, ObjectId? nodeId = null, List<ObjectId>? allowedNodeIds = null, DateTime? fromUtc = null, DateTime? toUtc = null, int limit = 50, CancellationToken ct = default);
    Task InsertAsync(IClientSessionHandle session, EnergyReservation reservation, CancellationToken ct = default);
    Task SaveAsync(EnergyReservation reservation, CancellationToken ct = default);
    Task SaveInTransactionAsync(IClientSessionHandle session, EnergyReservation reservation, CancellationToken ct = default);
    Task<long> CountActiveByProsumerAsync(string prosumerNic, CancellationToken ct = default);
    Task<long> CountActiveByNodeAsync(ObjectId nodeId, CancellationToken ct = default);
    Task<List<EnergyReservation>> GetPendingPastStartUtcAsync(DateTime nowUtc, CancellationToken ct = default);
    Task<List<EnergyReservation>> GetApprovedPastEndUtcAsync(DateTime nowUtc, CancellationToken ct = default);
}
