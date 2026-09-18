using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;
using SmartSolarMicrogrid.API.Data;
using SmartSolarMicrogrid.API.Models;
using SmartSolarMicrogrid.API.Repositories.Interfaces;

namespace SmartSolarMicrogrid.API.Repositories;

public class NodeRepository : INodeRepository
{
    private readonly MongoDbContext _context;

    public NodeRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<List<SolarStationInfo>> GetAllAsync(bool activeOnly = false, CancellationToken ct = default)
    {
        var filter = activeOnly
            ? Builders<SolarStationInfo>.Filter.Eq(n => n.Status, NodeStatus.Active)
            : Builders<SolarStationInfo>.Filter.Empty;

        return await _context.Nodes.Find(filter).ToListAsync(ct);
    }

    public async Task<List<SolarStationInfo>> GetNearbyAsync(double latitude, double longitude, double maxDistanceKm = 50, int limit = 10, CancellationToken ct = default)
    {
        try
        {
            var point = GeoJson.Point(GeoJson.Geographic(longitude, latitude));
            var filter = Builders<SolarStationInfo>.Filter.NearSphere(n => n.Location, point, maxDistanceKm * 1000)
                       & Builders<SolarStationInfo>.Filter.Eq(n => n.Status, NodeStatus.Active);

            return await _context.Nodes.Find(filter).Limit(limit).ToListAsync(ct);
        }
        catch
        {
            // Fallback to in-memory Haversine distance if 2dsphere index or GeoJson coordinate is missing
            var activeNodes = await _context.Nodes.Find(n => n.Status == NodeStatus.Active).ToListAsync(ct);
            return activeNodes
                .Select(n => new
                {
                    Node = n,
                    Distance = CalculateDistanceKm(latitude, longitude, n.Latitude, n.Longitude)
                })
                .Where(x => x.Distance <= maxDistanceKm)
                .OrderBy(x => x.Distance)
                .Take(limit)
                .Select(x => x.Node)
                .ToList();
        }
    }

    public async Task<SolarStationInfo?> GetByIdAsync(ObjectId id, CancellationToken ct = default)
    {
        return await _context.Nodes.Find(n => n.Id == id).FirstOrDefaultAsync(ct);
    }

    public async Task<SolarStationInfo?> GetByCodeAsync(string nodeCode, CancellationToken ct = default)
    {
        return await _context.Nodes.Find(n => n.NodeCode == nodeCode).FirstOrDefaultAsync(ct);
    }

    public async Task CreateAsync(SolarStationInfo node, CancellationToken ct = default)
    {
        if (node.Location == null && (node.Latitude != 0 || node.Longitude != 0))
        {
            node.Location = GeoJson.Point(GeoJson.Geographic(node.Longitude, node.Latitude));
        }
        await _context.Nodes.InsertOneAsync(node, cancellationToken: ct);
    }

    public async Task UpdateAsync(SolarStationInfo node, CancellationToken ct = default)
    {
        node.UpdatedAt = DateTime.UtcNow;
        if (node.Location == null && (node.Latitude != 0 || node.Longitude != 0))
        {
            node.Location = GeoJson.Point(GeoJson.Geographic(node.Longitude, node.Latitude));
        }
        await _context.Nodes.ReplaceOneAsync(n => n.Id == node.Id, node, cancellationToken: ct);
    }

    private static double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371; // Earth radius in km
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
