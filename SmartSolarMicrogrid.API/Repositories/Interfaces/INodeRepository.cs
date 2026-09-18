using MongoDB.Bson;
using SmartSolarMicrogrid.API.Models;

namespace SmartSolarMicrogrid.API.Repositories.Interfaces;

public interface INodeRepository
{
    Task<List<SolarStationInfo>> GetAllAsync(bool activeOnly = false, CancellationToken ct = default);
    Task<List<SolarStationInfo>> GetNearbyAsync(double latitude, double longitude, double maxDistanceKm = 50, int limit = 10, CancellationToken ct = default);
    Task<SolarStationInfo?> GetByIdAsync(ObjectId id, CancellationToken ct = default);
    Task<SolarStationInfo?> GetByCodeAsync(string nodeCode, CancellationToken ct = default);
    Task CreateAsync(SolarStationInfo node, CancellationToken ct = default);
    Task UpdateAsync(SolarStationInfo node, CancellationToken ct = default);
}
