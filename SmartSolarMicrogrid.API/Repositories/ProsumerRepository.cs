using MongoDB.Bson;
using MongoDB.Driver;
using SmartSolarMicrogrid.API.Data;
using SmartSolarMicrogrid.API.Models;
using SmartSolarMicrogrid.API.Repositories.Interfaces;

namespace SmartSolarMicrogrid.API.Repositories;

public class ProsumerRepository : IProsumerRepository
{
    private readonly IMongoCollection<Prosumer> _prosumers;

    public ProsumerRepository(MongoDbContext context)
    {
        _prosumers = context.Prosumers;
    }

    public async Task<Prosumer?> GetByIdAsync(string id)
    {
        if (!ObjectId.TryParse(id, out _)) return null;
        return await _prosumers.Find(p => p.Id == id).FirstOrDefaultAsync();
    }

    public async Task<Prosumer?> GetByNICAsync(string nic) =>
        await _prosumers.Find(p => p.NIC == nic.ToUpperInvariant()).FirstOrDefaultAsync();

    public async Task<Prosumer?> GetByEmailAsync(string email) =>
        await _prosumers.Find(p => p.Email == email.ToLowerInvariant()).FirstOrDefaultAsync();

    public async Task<List<Prosumer>> GetAllAsync() =>
        await _prosumers.Find(_ => true).SortByDescending(p => p.RegisteredAt).ToListAsync();

    public async Task<List<Prosumer>> GetByStatusAsync(string status) =>
        await _prosumers.Find(p => p.Status == status).SortByDescending(p => p.RegisteredAt).ToListAsync();

    public async Task CreateAsync(Prosumer prosumer)
    {
        prosumer.NIC   = prosumer.NIC.ToUpperInvariant();
        prosumer.Email = prosumer.Email.ToLowerInvariant();
        await _prosumers.InsertOneAsync(prosumer);
    }

    public async Task UpdateAsync(string id, Prosumer prosumer)
    {
        prosumer.UpdatedAt = DateTime.UtcNow;
        await _prosumers.ReplaceOneAsync(p => p.Id == id, prosumer);
    }

    public async Task DeleteAsync(string id) =>
        await _prosumers.DeleteOneAsync(p => p.Id == id);

    public async Task DeleteByNICAsync(string nic) =>
        await _prosumers.DeleteOneAsync(p => p.NIC == nic.ToUpperInvariant());

    public async Task<bool> NICExistsAsync(string nic) =>
        await _prosumers.Find(p => p.NIC == nic.ToUpperInvariant()).AnyAsync();

    public async Task<bool> EmailExistsAsync(string email) =>
        await _prosumers.Find(p => p.Email == email.ToLowerInvariant()).AnyAsync();
}
