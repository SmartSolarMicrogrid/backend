using SmartSolarMicrogrid.API.DTOs.Prosumer;
using SmartSolarMicrogrid.API.Models;
using SmartSolarMicrogrid.API.Repositories.Interfaces;
using SmartSolarMicrogrid.API.Services.Interfaces;
using SmartSolarMicrogrid.API.Utilities.Exceptions;

namespace SmartSolarMicrogrid.API.Services;

public class ProsumerService : IProsumerService
{
    private readonly IProsumerRepository _repo;

    public ProsumerService(IProsumerRepository repo)
    {
        _repo = repo;
    }

    public async Task<ProsumerDto> RegisterAsync(ProsumerRegisterDto request)
    {
        if (await _repo.NICExistsAsync(request.NIC))
            throw new ConflictException("NIC", request.NIC);

        if (await _repo.EmailExistsAsync(request.Email))
            throw new ConflictException("email", request.Email);

        var prosumer = new Prosumer
        {
            NIC          = request.NIC,
            FullName     = request.FullName,
            Email        = request.Email,
            Phone        = request.Phone,
            Address      = request.Address,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Status       = ProsumerStatus.Pending,
            RegisteredAt = DateTime.UtcNow,
        };

        await _repo.CreateAsync(prosumer);
        return ToDto(prosumer);
    }

    public async Task<List<ProsumerDto>> GetAllAsync() =>
        (await _repo.GetAllAsync()).Select(ToDto).ToList();

    public async Task<List<ProsumerDto>> GetPendingAsync() =>
        (await _repo.GetByStatusAsync(ProsumerStatus.Pending)).Select(ToDto).ToList();

    public async Task<ProsumerDto> GetByIdAsync(string id)
    {
        var p = await _repo.GetByIdAsync(id)
            ?? throw new NotFoundException("Prosumer", id);
        return ToDto(p);
    }

    public async Task<ProsumerDto> UpdateProfileAsync(string id, ProsumerUpdateDto request)
    {
        var p = await _repo.GetByIdAsync(id)
            ?? throw new NotFoundException("Prosumer", id);

        p.FullName  = request.FullName;
        p.Phone     = request.Phone;
        p.Address   = request.Address;
        p.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(id, p);
        return ToDto(p);
    }

    public async Task ActivateAsync(string id)
    {
        var p = await _repo.GetByIdAsync(id)
            ?? throw new NotFoundException("Prosumer", id);

        p.Status      = ProsumerStatus.Active;
        p.ActivatedAt = DateTime.UtcNow;
        p.UpdatedAt   = DateTime.UtcNow;

        await _repo.UpdateAsync(id, p);
    }

    public async Task DeactivateAsync(string id)
    {
        var p = await _repo.GetByIdAsync(id)
            ?? throw new NotFoundException("Prosumer", id);

        p.Status    = ProsumerStatus.Inactive;
        p.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(id, p);
    }

    public async Task RequestDeactivationAsync(string id)
    {
        var p = await _repo.GetByIdAsync(id)
            ?? throw new NotFoundException("Prosumer", id);

        if (p.Status != ProsumerStatus.Active)
            throw new BusinessRuleException("Only active prosumers can request deactivation.");

        p.Status    = ProsumerStatus.DeactivationRequested;
        p.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(id, p);
    }

    private static ProsumerDto ToDto(Prosumer p) => new()
    {
        Id           = p.Id,
        NIC          = p.NIC,
        FullName     = p.FullName,
        Email        = p.Email,
        Phone        = p.Phone,
        Address      = p.Address,
        Status       = p.Status,
        RegisteredAt = p.RegisteredAt,
        ActivatedAt  = p.ActivatedAt,
    };
}
