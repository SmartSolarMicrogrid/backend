using MongoDB.Bson;
using MongoDB.Driver;
using SmartSolarMicrogrid.API.Data;
using SmartSolarMicrogrid.API.Models;
using SmartSolarMicrogrid.API.Repositories.Interfaces;

namespace SmartSolarMicrogrid.API.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IMongoCollection<User> _users;

    public UserRepository(MongoDbContext context)
    {
        _users = context.Users;
    }

    public async Task<User?> GetByIdAsync(string id)
    {
        if (!ObjectId.TryParse(id, out _)) return null;
        return await _users.Find(u => u.Id == id).FirstOrDefaultAsync();
    }

    public async Task<User?> GetByEmailAsync(string email) =>
        await _users.Find(u => u.Email == email.ToLowerInvariant()).FirstOrDefaultAsync();

    public async Task<List<User>> GetAllAsync() =>
        await _users.Find(_ => true).SortBy(u => u.Name).ToListAsync();

    public async Task<List<User>> GetByRoleAsync(string role) =>
        await _users.Find(u => u.Role == role).SortBy(u => u.Name).ToListAsync();

    public async Task CreateAsync(User user)
    {
        user.Email = user.Email.ToLowerInvariant();
        await _users.InsertOneAsync(user);
    }

    public async Task UpdateAsync(string id, User user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        await _users.ReplaceOneAsync(u => u.Id == id, user);
    }

    public async Task DeleteAsync(string id) =>
        await _users.DeleteOneAsync(u => u.Id == id);

    public async Task<bool> EmailExistsAsync(string email) =>
        await _users.Find(u => u.Email == email.ToLowerInvariant()).AnyAsync();
}
