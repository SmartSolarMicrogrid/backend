using MongoDB.Bson;
using MongoDB.Driver;
using SmartSolarMicrogrid.API.Common.Errors;
using SmartSolarMicrogrid.API.Data;
using SmartSolarMicrogrid.API.DTOs.Nodes;
using SmartSolarMicrogrid.API.Models;
using SmartSolarMicrogrid.API.Repositories.Interfaces;
using SmartSolarMicrogrid.API.Services.Interfaces;

namespace SmartSolarMicrogrid.API.Services;

public class NodeService : INodeService
{
    private readonly INodeRepository _nodeRepository;
    private readonly MongoDbContext _context;

    public NodeService(INodeRepository nodeRepository, MongoDbContext context)
    {
        _nodeRepository = nodeRepository;
        _context = context;
    }

    public async Task<List<NodeResponseDto>> GetAllAsync(bool activeOnly = false, CancellationToken ct = default)
    {
        var nodes = await _nodeRepository.GetAllAsync(activeOnly, ct);
        return nodes.Select(MapToDto).ToList();
    }

    public async Task<List<NodeResponseDto>> GetNearbyAsync(double latitude, double longitude, double maxDistanceKm = 50, int limit = 10, CancellationToken ct = default)
    {
        var nodes = await _nodeRepository.GetNearbyAsync(latitude, longitude, maxDistanceKm, limit, ct);
        return nodes.Select(MapToDto).ToList();
    }

    private async Task<SolarStationInfo> FindNodeAsync(string id, CancellationToken ct)
    {
        SolarStationInfo? node = null;
        if (ObjectId.TryParse(id, out var objectId))
        {
            node = await _nodeRepository.GetByIdAsync(objectId, ct);
        }

        if (node == null)
        {
            node = await _nodeRepository.GetByCodeAsync(id, ct);
        }

        return node ?? throw new DomainException(ErrorCodes.NotFound, $"Node '{id}' was not found.");
    }

    public async Task<NodeResponseDto> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var node = await FindNodeAsync(id, ct);
        return MapToDto(node);
    }

    public async Task<NodeResponseDto> CreateAsync(CreateNodeDto request, CancellationToken ct = default)
    {
        var existing = await _nodeRepository.GetByCodeAsync(request.NodeCode, ct);
        if (existing != null)
            throw new DomainException(ErrorCodes.ValidationFailed, $"Node with code '{request.NodeCode}' already exists.");

        var node = new SolarStationInfo
        {
            Id = ObjectId.GenerateNewId(),
            NodeCode = request.NodeCode.ToUpperInvariant(),
            Name = request.Name,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Pricing = new NodePricing
            {
                BuyPricePerKwh = request.BuyPricePerKwh,
                SellPricePerKwh = request.SellPricePerKwh
            },
            OpeningHours = new OpeningHours
            {
                OpenTime = request.OpenTime,
                CloseTime = request.CloseTime,
                SlotDurationMinutes = request.SlotDurationMinutes > 0 ? request.SlotDurationMinutes : 60
            },
            CapacityBays = request.CapacityBays,
            MaxKwhPerReservation = request.MaxKwhPerReservation,
            OperatorIds = request.OperatorIds ?? new List<string>(),
            Status = NodeStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        await _nodeRepository.CreateAsync(node, ct);
        return MapToDto(node);
    }

    public async Task<NodeResponseDto> UpdateAsync(string id, UpdateNodeDto request, CancellationToken ct = default)
    {
        var node = await FindNodeAsync(id, ct);

        node.Name = request.Name;
        node.Latitude = request.Latitude;
        node.Longitude = request.Longitude;
        node.Pricing.BuyPricePerKwh = request.BuyPricePerKwh;
        node.Pricing.SellPricePerKwh = request.SellPricePerKwh;
        node.OpeningHours.OpenTime = request.OpenTime;
        node.OpeningHours.CloseTime = request.CloseTime;
        node.OpeningHours.SlotDurationMinutes = request.SlotDurationMinutes > 0 ? request.SlotDurationMinutes : 60;
        if (request.CapacityBays > 0) node.CapacityBays = request.CapacityBays;
        if (request.MaxKwhPerReservation > 0)
            node.MaxKwhPerReservation = request.MaxKwhPerReservation;
        else if (node.MaxKwhPerReservation <= 0)
            node.MaxKwhPerReservation = 50m;
        node.OperatorIds = request.OperatorIds ?? new List<string>();

        await _nodeRepository.UpdateAsync(node, ct);
        return MapToDto(node);
    }

    public async Task DeactivateAsync(string id, CancellationToken ct = default)
    {
        var node = await FindNodeAsync(id, ct);

        // BR-06: Check active reservations whose slot has not ended
        var activeCount = await _context.Reservations.CountDocumentsAsync(
            r => r.NodeId == node.Id && r.IsActive && r.SlotEndUtc > DateTime.UtcNow, cancellationToken: ct);

        if (activeCount > 0)
            throw new DomainException(ErrorCodes.NodeHasActiveReservations, $"Cannot deactivate node: {activeCount} active reservation(s) exist.");

        node.Status = NodeStatus.Inactive;
        await _nodeRepository.UpdateAsync(node, ct);
    }

    public async Task ActivateAsync(string id, CancellationToken ct = default)
    {
        var node = await FindNodeAsync(id, ct);

        node.Status = NodeStatus.Active;
        await _nodeRepository.UpdateAsync(node, ct);
    }

    private static NodeResponseDto MapToDto(SolarStationInfo node) => new()
    {
        Id = node.Id.ToString(),
        NodeCode = node.NodeCode,
        Name = node.Name,
        Latitude = node.Latitude,
        Longitude = node.Longitude,
        BuyPricePerKwh = node.Pricing.BuyPricePerKwh,
        SellPricePerKwh = node.Pricing.SellPricePerKwh,
        OpenTime = node.OpeningHours.OpenTime,
        CloseTime = node.OpeningHours.CloseTime,
        SlotDurationMinutes = node.OpeningHours.SlotDurationMinutes,
        Status = node.Status.ToString(),
        CapacityBays = node.CapacityBays,
        MaxKwhPerReservation = node.MaxKwhPerReservation,
        OperatorIds = node.OperatorIds,
        CreatedAt = node.CreatedAt
    };
}
