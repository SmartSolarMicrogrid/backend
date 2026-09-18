using MongoDB.Driver;
using SmartSolarMicrogrid.API.Auth;
using SmartSolarMicrogrid.API.Models;

namespace SmartSolarMicrogrid.API.Data;

/// <summary>
/// Seeds the database with default data on first startup.
/// Creates the initial Backoffice admin if no users exist.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(MongoDbContext context)
    {
        // ── Unique indexes ──────────────────────────────────────────
        var userEmailIndex = new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(u => u.Email),
            new CreateIndexOptions { Unique = true });
        await context.Users.Indexes.CreateOneAsync(userEmailIndex);

        var prosumerNicIndex = new CreateIndexModel<Prosumer>(
            Builders<Prosumer>.IndexKeys.Ascending(p => p.NIC),
            new CreateIndexOptions { Unique = true });
        await context.Prosumers.Indexes.CreateOneAsync(prosumerNicIndex);

        var prosumerEmailIndex = new CreateIndexModel<Prosumer>(
            Builders<Prosumer>.IndexKeys.Ascending(p => p.Email),
            new CreateIndexOptions { Unique = true });
        await context.Prosumers.Indexes.CreateOneAsync(prosumerEmailIndex);

        var prosumerStatusIndex = new CreateIndexModel<Prosumer>(
            Builders<Prosumer>.IndexKeys.Ascending(p => p.Status));
        await context.Prosumers.Indexes.CreateOneAsync(prosumerStatusIndex);

        // ── Seed default admin if collection is empty ────────────────
        var userCount = await context.Users.CountDocumentsAsync(FilterDefinition<User>.Empty);
        if (userCount == 0)
        {
            var admin = new User
            {
                Name         = "System Admin",
                Email        = "admin@solarmicrogrid.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@1234"),
                Role         = RoleConstants.Backoffice,
                IsActive     = true,
                CreatedAt    = DateTime.UtcNow,
            };
            await context.Users.InsertOneAsync(admin);
            Console.WriteLine("[Seeder] Default Backoffice admin created: admin@solarmicrogrid.com");
        }
    }
}
