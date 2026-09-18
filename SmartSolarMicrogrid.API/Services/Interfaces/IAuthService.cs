using System.Security.Claims;
using SmartSolarMicrogrid.API.DTOs.Auth;
using SmartSolarMicrogrid.API.DTOs.Prosumer;

namespace SmartSolarMicrogrid.API.Services.Interfaces;

public interface IAuthService
{
    /// <summary>
    /// Authenticates a user (web user or prosumer) and returns a JWT.
    /// Checks Users collection first, then Prosumers.
    /// Prosumers with Status != Active are rejected.
    /// </summary>
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request);

    /// <summary>
    /// Self-registration for prosumers.
    /// </summary>
    Task<ProsumerDto> RegisterAsync(ProsumerRegisterDto request);

    /// <summary>
    /// Gets the profile of the currently signed-in user or prosumer.
    /// </summary>
    Task<CurrentUserDto> GetCurrentUserAsync(ClaimsPrincipal userPrincipal);
}
