using MongoDB.Bson;
using MongoDB.Driver;
using SmartSolarMicrogrid.API.Models;

namespace SmartSolarMicrogrid.API.Data;

public class IndexInitializer
{
    private readonly MongoDbContext _db;
    private readonly ILogger<IndexInitializer> _logger;

    public IndexInitializer(MongoDbContext db, ILogger<IndexInitializer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        // 1. UserDetails: email unique
        await SafeCreateIndexAsync(
            _db.Users,
            new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(u => u.Email),
                new CreateIndexOptions { Unique = true }),
            ct);

        // 2. UserDetails: role, isActive
        await SafeCreateIndexAsync(
            _db.Users,
            new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(u => u.Role).Ascending(u => u.IsActive),
                new CreateIndexOptions { Name = "ix_user_role_status" }),
            ct);

        // 3. Prosumer: nic unique
        await SafeCreateIndexAsync(
            _db.Prosumers,
            new CreateIndexModel<Prosumer>(
                Builders<Prosumer>.IndexKeys.Ascending(p => p.NIC),
                new CreateIndexOptions { Unique = true }),
            ct);

        // 4. SolarStationInfo: nodeCode unique
        await SafeCreateIndexAsync(
            _db.Nodes,
            new CreateIndexModel<SolarStationInfo>(
                Builders<SolarStationInfo>.IndexKeys.Ascending(n => n.NodeCode),
                new CreateIndexOptions { Unique = true, Name = "ux_node_code" }),
            ct);

        // 5. SolarStationInfo: location 2dsphere
        await SafeCreateIndexAsync(
            _db.Nodes,
            new CreateIndexModel<SolarStationInfo>(
                Builders<SolarStationInfo>.IndexKeys.Geo2DSphere(n => n.Location),
                new CreateIndexOptions { Name = "ix_location_2dsphere", Sparse = true }),
            ct);

        // 6. EnergyBookingSlots: nodeId, startUtc unique
        await SafeCreateIndexAsync(
            _db.Slots,
            new CreateIndexModel<EnergyBookingSlot>(
                Builders<EnergyBookingSlot>.IndexKeys.Ascending(s => s.NodeId).Ascending(s => s.StartUtc),
                new CreateIndexOptions { Unique = true, Name = "ux_slot_node_start" }),
            ct);

        // 7. EnergyReservation: slotId, prosumerNic unique partial where isActive is true
        await SafeCreateIndexAsync(
            _db.Reservations,
            new CreateIndexModel<EnergyReservation>(
                Builders<EnergyReservation>.IndexKeys.Ascending(r => r.SlotId).Ascending(r => r.ProsumerNic),
                new CreateIndexOptions<EnergyReservation>
                {
                    Name = "ux_slot_prosumer_active",
                    Unique = true,
                    PartialFilterExpression = Builders<EnergyReservation>.Filter.Eq(r => r.IsActive, true)
                }),
            ct);

        // 8. EnergyReservation: prosumerNic, slotStartUtc descending
        await SafeCreateIndexAsync(
            _db.Reservations,
            new CreateIndexModel<EnergyReservation>(
                Builders<EnergyReservation>.IndexKeys.Ascending(r => r.ProsumerNic).Descending(r => r.SlotStartUtc),
                new CreateIndexOptions { Name = "ix_reservation_prosumer_time" }),
            ct);

        // 9. EnergyReservation: nodeId, slotStartUtc
        await SafeCreateIndexAsync(
            _db.Reservations,
            new CreateIndexModel<EnergyReservation>(
                Builders<EnergyReservation>.IndexKeys.Ascending(r => r.NodeId).Ascending(r => r.SlotStartUtc),
                new CreateIndexOptions { Name = "ix_reservation_node_time" }),
            ct);

        // 10. EnergyReservation: reservationNo unique
        await SafeCreateIndexAsync(
            _db.Reservations,
            new CreateIndexModel<EnergyReservation>(
                Builders<EnergyReservation>.IndexKeys.Ascending(r => r.ReservationNo),
                new CreateIndexOptions { Unique = true, Name = "ux_reservation_no" }),
            ct);

        _logger.LogInformation("MongoDB indexes verified successfully.");
    }

    private async Task SafeCreateIndexAsync<T>(IMongoCollection<T> collection, CreateIndexModel<T> model, CancellationToken ct)
    {
        try
        {
            await collection.Indexes.CreateOneAsync(model, cancellationToken: ct);
        }
        catch (MongoCommandException ex) when (ex.CodeName == "IndexOptionsConflict" || ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        {
            // Index exists with matching or default name - safe to proceed
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Notice while ensuring index on {Collection}: {Message}", typeof(T).Name, ex.Message);
        }
    }
}
