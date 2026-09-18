using SmartSolarMicrogrid.API.Models;

namespace SmartSolarMicrogrid.API.Repositories.Interfaces;

public interface IProsumerRepository
{
    Task<Prosumer?> GetByIdAsync(string id);
    Task<Prosumer?> GetByNICAsync(string nic);
    Task<Prosumer?> GetByEmailAsync(string email);
    Task<List<Prosumer>> GetAllAsync();
    Task<List<Prosumer>> GetByStatusAsync(string status);
    Task CreateAsync(Prosumer prosumer);
    Task UpdateAsync(string id, Prosumer prosumer);
    Task<bool> NICExistsAsync(string nic);
    Task<bool> EmailExistsAsync(string email);
}
