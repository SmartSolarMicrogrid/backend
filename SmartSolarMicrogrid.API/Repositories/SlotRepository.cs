using MongoDB.Bson;
using MongoDB.Driver;
using SmartSolarMicrogrid.API.Data;
using SmartSolarMicrogrid.API.Models;
using SmartSolarMicrogrid.API.Repositories.Interfaces;

namespace SmartSolarMicrogrid.API.Repositories;

public class SlotRepository : ISlotRepository
{
    private readonly MongoDbContext _context;

    public SlotRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<List<EnergyBookingSlot>> GetByNodeAndDayAsync(ObjectId nodeId, string localDate, CancellationToken ct = default)
    {
        var filter = Builders<EnergyBookingSlot>.Filter.Eq(s => s.NodeId, nodeId)
                   & Builders<EnergyBookingSlot>.Filter.Eq(s => s.LocalDate, localDate);

        return await _context.Slots.Find(filter).SortBy(s => s.StartUtc).ToListAsync(ct);
    }

    public async Task<EnergyBookingSlot?> GetByIdAsync(ObjectId slotId, CancellationToken ct = default)
    {
        return await _context.Slots.Find(s => s.Id == slotId).FirstOrDefaultAsync(ct);
    }

    public async Task<bool> TryHoldBayAsync(IClientSessionHandle session, ObjectId slotId, CancellationToken ct = default)
    {
        var filter = Builders<EnergyBookingSlot>.Filter.Eq(s => s.Id, slotId)
                   & Builders<EnergyBookingSlot>.Filter.Eq(s => s.Status, SlotStatus.Available)
                   & new BsonDocumentFilterDefinition<EnergyBookingSlot>(
                       BsonDocument.Parse("{ $expr: { $lt: ['$bookedCount', '$capacity'] } }"));

        var update = Builders<EnergyBookingSlot>.Update.Inc(s => s.BookedCount, 1);
        var result = await _context.Slots.UpdateOneAsync(session, filter, update, cancellationToken: ct);

        return result.ModifiedCount == 1;
    }

    public async Task ReleaseBayAsync(IClientSessionHandle session, ObjectId slotId, CancellationToken ct = default)
    {
        var filter = Builders<EnergyBookingSlot>.Filter.Eq(s => s.Id, slotId)
                   & Builders<EnergyBookingSlot>.Filter.Gt(s => s.BookedCount, 0);

        var update = Builders<EnergyBookingSlot>.Update.Inc(s => s.BookedCount, -1);
        await _context.Slots.UpdateOneAsync(session, filter, update, cancellationToken: ct);
    }

    public async Task UpdateAsync(EnergyBookingSlot slot, CancellationToken ct = default)
    {
        slot.Version++;
        await _context.Slots.ReplaceOneAsync(s => s.Id == slot.Id, slot, cancellationToken: ct);
    }

    public async Task UpsertSlotsAsync(IEnumerable<EnergyBookingSlot> slots, CancellationToken ct = default)
    {
        var writes = new List<WriteModel<EnergyBookingSlot>>();
        foreach (var slot in slots)
        {
            var filter = Builders<EnergyBookingSlot>.Filter.Eq(s => s.NodeId, slot.NodeId)
                       & Builders<EnergyBookingSlot>.Filter.Eq(s => s.StartUtc, slot.StartUtc);

            var update = Builders<EnergyBookingSlot>.Update
                .SetOnInsert(s => s.NodeId, slot.NodeId)
                .SetOnInsert(s => s.StartUtc, slot.StartUtc)
                .SetOnInsert(s => s.EndUtc, slot.EndUtc)
                .SetOnInsert(s => s.LocalDate, slot.LocalDate)
                .SetOnInsert(s => s.Capacity, slot.Capacity)
                .SetOnInsert(s => s.BookedCount, 0)
                .SetOnInsert(s => s.Status, slot.Status)
                .SetOnInsert(s => s.Version, 1);

            var upsert = new UpdateOneModel<EnergyBookingSlot>(filter, update) { IsUpsert = true };
            writes.Add(upsert);
        }

        if (writes.Count > 0)
        {
            await _context.Slots.BulkWriteAsync(writes, new BulkWriteOptions { IsOrdered = false }, ct);
        }
    }

    public async Task<bool> ExistsForTimeAsync(ObjectId nodeId, DateTime startUtc, CancellationToken ct = default)
    {
        var filter = Builders<EnergyBookingSlot>.Filter.Eq(s => s.NodeId, nodeId)
                   & Builders<EnergyBookingSlot>.Filter.Eq(s => s.StartUtc, startUtc);

        return await _context.Slots.Find(filter).AnyAsync(ct);
    }
}
