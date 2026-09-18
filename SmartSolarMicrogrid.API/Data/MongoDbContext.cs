using MongoDB.Driver;
using SmartSolarMicrogrid.API.Models;

namespace SmartSolarMicrogrid.API.Data;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(MongoDbSettings settings)
    {
        var client = new MongoClient(settings.ConnectionString);
        _database  = client.GetDatabase(settings.DatabaseName);
    }

    public IMongoCollection<User>     Users     => _database.GetCollection<User>("UserDetails");
    public IMongoCollection<Prosumer> Prosumers => _database.GetCollection<Prosumer>("Prosumers");

    // Future collections will be added here as modules are implemented:
    // public IMongoCollection<SolarStationInfo>  SolarStations  => ...
    // public IMongoCollection<EnergyBookingSlot> BookingSlots   => ...
    // public IMongoCollection<EnergyReservation> Reservations   => ...
}
