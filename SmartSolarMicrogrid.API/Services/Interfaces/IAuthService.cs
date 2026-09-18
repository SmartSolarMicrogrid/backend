using SmartSolarMicrogrid.API.DTOs.Auth;

namespace SmartSolarMicrogrid.API.Services.Interfaces;

public interface IAuthService
{
    /// <summary>
    /// Authenticates a user (web user or prosumer) and returns a JWT.
    /// Checks Users collection first, then Prosumers.
    /// Prosumers with Status != Active are rejected.
    /// </summary>
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request);
}
