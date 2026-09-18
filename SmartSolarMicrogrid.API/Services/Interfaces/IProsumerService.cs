using SmartSolarMicrogrid.API.DTOs.Prosumer;

namespace SmartSolarMicrogrid.API.Services.Interfaces;

public interface IProsumerService
{
    Task<ProsumerDto> RegisterAsync(ProsumerRegisterDto request);
    Task<ProsumerDto> CreateByBackofficeAsync(CreateProsumerDto request);
    Task<List<ProsumerDto>> GetAllAsync();
    Task<List<ProsumerDto>> GetPendingAsync();
    Task<ProsumerDto> GetByIdAsync(string id);
    Task<ProsumerDto> GetByNicOrIdAsync(string identifier);
    Task<ProsumerDto> UpdateProfileAsync(string id, ProsumerUpdateDto request);
    Task<ProsumerDto> UpdateByNicOrIdAsync(string identifier, ProsumerUpdateDto request);
    Task DeleteByNicOrIdAsync(string identifier);
    Task ActivateAsync(string id);
    Task DeactivateAsync(string id);
    Task DeactivateByNicOrIdAsync(string identifier);
    Task ReactivateByNicOrIdAsync(string identifier);
    Task RequestDeactivationAsync(string id);
}
