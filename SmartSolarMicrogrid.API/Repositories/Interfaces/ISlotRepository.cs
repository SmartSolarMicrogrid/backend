using MongoDB.Bson;
using MongoDB.Driver;
using SmartSolarMicrogrid.API.Models;

namespace SmartSolarMicrogrid.API.Repositories.Interfaces;

public interface ISlotRepository
{
    Task<List<EnergyBookingSlot>> GetByNodeAndDayAsync(ObjectId nodeId, string localDate, CancellationToken ct = default);
    Task<EnergyBookingSlot?> GetByIdAsync(ObjectId slotId, CancellationToken ct = default);
    Task<bool> TryHoldBayAsync(IClientSessionHandle session, ObjectId slotId, CancellationToken ct = default);
    Task ReleaseBayAsync(IClientSessionHandle session, ObjectId slotId, CancellationToken ct = default);
    Task UpdateAsync(EnergyBookingSlot slot, CancellationToken ct = default);
    Task UpsertSlotsAsync(IEnumerable<EnergyBookingSlot> slots, CancellationToken ct = default);
    Task<bool> ExistsForTimeAsync(ObjectId nodeId, DateTime startUtc, CancellationToken ct = default);
}
