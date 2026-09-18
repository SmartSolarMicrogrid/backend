using SmartSolarMicrogrid.API.DTOs.Prosumer;

namespace SmartSolarMicrogrid.API.Services.Interfaces;

public interface IProsumerService
{
    Task<ProsumerDto> RegisterAsync(ProsumerRegisterDto request);
    Task<List<ProsumerDto>> GetAllAsync();
    Task<List<ProsumerDto>> GetPendingAsync();
    Task<ProsumerDto> GetByIdAsync(string id);
    Task<ProsumerDto> UpdateProfileAsync(string id, ProsumerUpdateDto request);
    Task ActivateAsync(string id);
    Task DeactivateAsync(string id);
    Task RequestDeactivationAsync(string id);
}
