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
        try
        {
            // 1. UserDetails: email unique
            await _db.Users.Indexes.CreateOneAsync(
                new CreateIndexModel<User>(
                    Builders<User>.IndexKeys.Ascending(u => u.Email),
                    new CreateIndexOptions { Unique = true, Name = "ux_user_email" }),
                cancellationToken: ct);

            // 2. UserDetails: role, isActive
            await _db.Users.Indexes.CreateOneAsync(
                new CreateIndexModel<User>(
                    Builders<User>.IndexKeys.Ascending(u => u.Role).Ascending(u => u.IsActive),
                    new CreateIndexOptions { Name = "ix_user_role_status" }),
                cancellationToken: ct);

            // 3. Prosumer: nic unique
            await _db.Prosumers.Indexes.CreateOneAsync(
                new CreateIndexModel<Prosumer>(
                    Builders<Prosumer>.IndexKeys.Ascending(p => p.NIC),
                    new CreateIndexOptions { Unique = true, Name = "ux_prosumer_nic" }),
                cancellationToken: ct);

            // 4. SolarStationInfo: nodeCode unique
            await _db.Nodes.Indexes.CreateOneAsync(
                new CreateIndexModel<SolarStationInfo>(
                    Builders<SolarStationInfo>.IndexKeys.Ascending(n => n.NodeCode),
                    new CreateIndexOptions { Unique = true, Name = "ux_node_code" }),
                cancellationToken: ct);

            // 5. SolarStationInfo: location 2dsphere
            try
            {
                await _db.Nodes.Indexes.CreateOneAsync(
                    new CreateIndexModel<SolarStationInfo>(
                        Builders<SolarStationInfo>.IndexKeys.Geo2DSphere(n => n.Location),
                        new CreateIndexOptions { Name = "ix_location_2dsphere", Sparse = true }),
                    cancellationToken: ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Geospatial index notice: {Message}", ex.Message);
            }

            // 6. EnergyBookingSlots: nodeId, startUtc unique
            await _db.Slots.Indexes.CreateOneAsync(
                new CreateIndexModel<EnergyBookingSlot>(
                    Builders<EnergyBookingSlot>.IndexKeys.Ascending(s => s.NodeId).Ascending(s => s.StartUtc),
                    new CreateIndexOptions { Unique = true, Name = "ux_slot_node_start" }),
                cancellationToken: ct);

            // 7. EnergyReservation: slotId, prosumerNic unique partial where isActive is true
            await _db.Reservations.Indexes.CreateOneAsync(
                new CreateIndexModel<EnergyReservation>(
                    Builders<EnergyReservation>.IndexKeys.Ascending(r => r.SlotId).Ascending(r => r.ProsumerNic),
                    new CreateIndexOptions<EnergyReservation>
                    {
                        Name = "ux_slot_prosumer_active",
                        Unique = true,
                        PartialFilterExpression = Builders<EnergyReservation>.Filter.Eq(r => r.IsActive, true)
                    }),
                cancellationToken: ct);

            // 8. EnergyReservation: prosumerNic, slotStartUtc descending
            await _db.Reservations.Indexes.CreateOneAsync(
                new CreateIndexModel<EnergyReservation>(
                    Builders<EnergyReservation>.IndexKeys.Ascending(r => r.ProsumerNic).Descending(r => r.SlotStartUtc),
                    new CreateIndexOptions { Name = "ix_reservation_prosumer_time" }),
                cancellationToken: ct);

            // 9. EnergyReservation: nodeId, slotStartUtc
            await _db.Reservations.Indexes.CreateOneAsync(
                new CreateIndexModel<EnergyReservation>(
                    Builders<EnergyReservation>.IndexKeys.Ascending(r => r.NodeId).Ascending(r => r.SlotStartUtc),
                    new CreateIndexOptions { Name = "ix_reservation_node_time" }),
                cancellationToken: ct);

            // 10. EnergyReservation: reservationNo unique
            await _db.Reservations.Indexes.CreateOneAsync(
                new CreateIndexModel<EnergyReservation>(
                    Builders<EnergyReservation>.IndexKeys.Ascending(r => r.ReservationNo),
                    new CreateIndexOptions { Unique = true, Name = "ux_reservation_no" }),
                cancellationToken: ct);

            _logger.LogInformation("MongoDB indexes initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize MongoDB indexes");
        }
    }
}
