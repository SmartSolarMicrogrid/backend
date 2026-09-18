using SmartSolarMicrogrid.API.Models;

namespace SmartSolarMicrogrid.API.Repositories.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(string id);
    Task<User?> GetByEmailAsync(string email);
    Task<List<User>> GetAllAsync();
    Task<List<User>> GetByRoleAsync(string role);
    Task CreateAsync(User user);
    Task UpdateAsync(string id, User user);
    Task DeleteAsync(string id);
    Task<bool> EmailExistsAsync(string email);
}
