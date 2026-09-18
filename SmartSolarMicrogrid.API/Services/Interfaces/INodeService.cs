using SmartSolarMicrogrid.API.DTOs.Nodes;

namespace SmartSolarMicrogrid.API.Services.Interfaces;

public interface INodeService
{
    Task<List<NodeResponseDto>> GetAllAsync(bool activeOnly = false, CancellationToken ct = default);
    Task<List<NodeResponseDto>> GetNearbyAsync(double latitude, double longitude, double maxDistanceKm = 50, int limit = 10, CancellationToken ct = default);
    Task<NodeResponseDto> GetByIdAsync(string id, CancellationToken ct = default);
    Task<NodeResponseDto> CreateAsync(CreateNodeDto request, CancellationToken ct = default);
    Task<NodeResponseDto> UpdateAsync(string id, UpdateNodeDto request, CancellationToken ct = default);
    Task DeactivateAsync(string id, CancellationToken ct = default);
    Task ActivateAsync(string id, CancellationToken ct = default);
}
