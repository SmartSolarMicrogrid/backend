using MongoDB.Driver;
using SmartSolarMicrogrid.API.Models;

namespace SmartSolarMicrogrid.API.Data;

public class MongoDbContext
{
    private readonly IMongoClient _client;
    private readonly IMongoDatabase _database;

    public MongoDbContext(MongoDbSettings settings)
    {
        _client = new MongoClient(settings.ConnectionString);
        _database = _client.GetDatabase(settings.DatabaseName);
    }

    public IMongoClient Client => _client;
    public IMongoDatabase Database => _database;

    public IMongoCollection<User> Users => _database.GetCollection<User>("UserDetails");
    public IMongoCollection<Prosumer> Prosumers => _database.GetCollection<Prosumer>("Prosumers");
    public IMongoCollection<SolarStationInfo> Nodes => _database.GetCollection<SolarStationInfo>("SolarStationInfo");
    public IMongoCollection<EnergyBookingSlot> Slots => _database.GetCollection<EnergyBookingSlot>("EnergyBookingSlots");
    public IMongoCollection<EnergyReservation> Reservations => _database.GetCollection<EnergyReservation>("EnergyReservation");
}
