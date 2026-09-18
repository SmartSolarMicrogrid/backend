using SmartSolarMicrogrid.API.DTOs.Auth;
using SmartSolarMicrogrid.API.DTOs.User;

namespace SmartSolarMicrogrid.API.Services.Interfaces;

public interface IUserService
{
    Task<List<UserDto>> GetAllUsersAsync();
    Task<UserDto> GetUserByIdAsync(string id);
    Task<UserDto> CreateUserAsync(RegisterRequestDto request);
    Task<UserDto> UpdateUserAsync(string id, UpdateUserDto request);
    Task<UserDto> SetStatusAsync(string id, bool isActive);
    Task DeleteUserAsync(string id);
    Task ChangePasswordAsync(string id, ChangePasswordDto request);
}
