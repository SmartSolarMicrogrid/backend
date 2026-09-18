using SmartSolarMicrogrid.API.Auth;
using SmartSolarMicrogrid.API.DTOs.Auth;
using SmartSolarMicrogrid.API.DTOs.User;
using SmartSolarMicrogrid.API.Models;
using SmartSolarMicrogrid.API.Repositories.Interfaces;
using SmartSolarMicrogrid.API.Services.Interfaces;
using SmartSolarMicrogrid.API.Utilities.Exceptions;

namespace SmartSolarMicrogrid.API.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _repo;

    public UserService(IUserRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<UserDto>> GetAllUsersAsync() =>
        (await _repo.GetAllAsync()).Select(ToDto).ToList();

    public async Task<UserDto> GetUserByIdAsync(string id)
    {
        var user = await _repo.GetByIdAsync(id)
            ?? throw new NotFoundException("User", id);
        return ToDto(user);
    }

    public async Task<UserDto> CreateUserAsync(RegisterRequestDto request)
    {
        if (await _repo.EmailExistsAsync(request.Email))
            throw new ConflictException("email", request.Email);

        var user = new User
        {
            Name         = request.Name,
            Email        = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role         = request.Role,
            IsActive     = true,
            CreatedAt    = DateTime.UtcNow,
        };

        await _repo.CreateAsync(user);
        return ToDto(user);
    }

    public async Task<UserDto> UpdateUserAsync(string id, UpdateUserDto request)
    {
        var user = await _repo.GetByIdAsync(id)
            ?? throw new NotFoundException("User", id);

        // Validate role
        string[] validRoles = [RoleConstants.Backoffice, RoleConstants.GridOperator];
        if (!validRoles.Contains(request.Role))
            throw new BusinessRuleException($"Role must be one of: {string.Join(", ", validRoles)}.");

        user.Name      = request.Name;
        user.Role      = request.Role;
        user.IsActive  = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(id, user);
        return ToDto(user);
    }

    public async Task DeleteUserAsync(string id)
    {
        var user = await _repo.GetByIdAsync(id)
            ?? throw new NotFoundException("User", id);
        await _repo.DeleteAsync(id);
    }

    public async Task ChangePasswordAsync(string id, ChangePasswordDto request)
    {
        var user = await _repo.GetByIdAsync(id)
            ?? throw new NotFoundException("User", id);

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedException("Current password is incorrect.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt    = DateTime.UtcNow;

        await _repo.UpdateAsync(id, user);
    }

    private static UserDto ToDto(User u) => new()
    {
        Id        = u.Id,
        Name      = u.Name,
        Email     = u.Email,
        Role      = u.Role,
        IsActive  = u.IsActive,
        CreatedAt = u.CreatedAt,
    };
}
