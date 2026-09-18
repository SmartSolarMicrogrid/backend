using System.Security.Claims;
using SmartSolarMicrogrid.API.Auth;
using SmartSolarMicrogrid.API.DTOs.Auth;
using SmartSolarMicrogrid.API.DTOs.Prosumer;
using SmartSolarMicrogrid.API.Models;
using SmartSolarMicrogrid.API.Repositories.Interfaces;
using SmartSolarMicrogrid.API.Services.Interfaces;
using SmartSolarMicrogrid.API.Utilities.Exceptions;

namespace SmartSolarMicrogrid.API.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository     _userRepo;
    private readonly IProsumerRepository _prosumerRepo;
    private readonly IProsumerService    _prosumerService;
    private readonly JwtTokenGenerator   _jwtGenerator;

    public AuthService(
        IUserRepository     userRepo,
        IProsumerRepository prosumerRepo,
        IProsumerService    prosumerService,
        JwtTokenGenerator   jwtGenerator)
    {
        _userRepo        = userRepo;
        _prosumerRepo    = prosumerRepo;
        _prosumerService = prosumerService;
        _jwtGenerator    = jwtGenerator;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        // ── 1. Try web app users (Backoffice / GridOperator) ────────
        var user = await _userRepo.GetByEmailAsync(request.Email);
        if (user is not null)
        {
            if (!user.IsActive)
                throw new UnauthorizedException("Your account has been deactivated.");

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new UnauthorizedException("Invalid email or password.");

            var (token, expiresAt) = _jwtGenerator.GenerateForUser(user.Id, user.Email, user.Role);

            return new AuthResponseDto
            {
                Token        = token,
                ExpiresAt    = expiresAt,
                UserId       = user.Id,
                Name         = user.Name,
                Email        = user.Email,
                Role         = user.Role,
                RedirectPath = GetRedirectPath(user.Role),
            };
        }

        // ── 2. Try Prosumers ────────────────────────────────────────
        var prosumer = await _prosumerRepo.GetByEmailAsync(request.Email);
        if (prosumer is not null)
        {
            if (prosumer.Status == ProsumerStatus.Pending)
                throw new UnauthorizedException("Your account is pending activation. Please wait for Backoffice approval.");

            if (prosumer.Status == ProsumerStatus.Inactive || prosumer.Status == ProsumerStatus.DeactivationRequested)
                throw new UnauthorizedException("Your account has been deactivated.");

            if (!BCrypt.Net.BCrypt.Verify(request.Password, prosumer.PasswordHash))
                throw new UnauthorizedException("Invalid email or password.");

            var (token, expiresAt) = _jwtGenerator.GenerateForProsumer(prosumer.Id, prosumer.Email, prosumer.NIC);

            return new AuthResponseDto
            {
                Token        = token,
                ExpiresAt    = expiresAt,
                UserId       = prosumer.Id,
                Name         = prosumer.FullName,
                Email        = prosumer.Email,
                Role         = RoleConstants.Prosumer,
                RedirectPath = GetRedirectPath(RoleConstants.Prosumer),
            };
        }

        throw new UnauthorizedException("Invalid email or password.");
    }

    public async Task<ProsumerDto> RegisterAsync(ProsumerRegisterDto request) =>
        await _prosumerService.RegisterAsync(request);

    public async Task<CurrentUserDto> GetCurrentUserAsync(ClaimsPrincipal userPrincipal)
    {
        var userId = userPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? userPrincipal.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException("User identifier not found in token.");

        var role = userPrincipal.FindFirstValue(ClaimTypes.Role)
                ?? userPrincipal.FindFirstValue("role")
                ?? string.Empty;

        if (role == RoleConstants.Prosumer)
        {
            var prosumer = await _prosumerRepo.GetByIdAsync(userId)
                        ?? await _prosumerRepo.GetByNICAsync(userId)
                        ?? throw new NotFoundException("Prosumer", userId);

            return new CurrentUserDto
            {
                Id           = prosumer.Id,
                Email        = prosumer.Email,
                Name         = prosumer.FullName,
                Role         = RoleConstants.Prosumer,
                NIC          = prosumer.NIC,
                Phone        = prosumer.Phone,
                Address      = prosumer.Address,
                Status       = prosumer.Status,
                IsActive     = prosumer.Status == ProsumerStatus.Active,
                RegisteredAt = prosumer.RegisteredAt
            };
        }

        var user = await _userRepo.GetByIdAsync(userId);
        if (user is not null)
        {
            return new CurrentUserDto
            {
                Id           = user.Id,
                Email        = user.Email,
                Name         = user.Name,
                Role         = user.Role,
                Status       = user.IsActive ? "Active" : "Inactive",
                IsActive     = user.IsActive,
                RegisteredAt = user.CreatedAt
            };
        }

        var pFallback = await _prosumerRepo.GetByIdAsync(userId)
                     ?? await _prosumerRepo.GetByNICAsync(userId);
        if (pFallback is not null)
        {
            return new CurrentUserDto
            {
                Id           = pFallback.Id,
                Email        = pFallback.Email,
                Name         = pFallback.FullName,
                Role         = RoleConstants.Prosumer,
                NIC          = pFallback.NIC,
                Phone        = pFallback.Phone,
                Address      = pFallback.Address,
                Status       = pFallback.Status,
                IsActive     = pFallback.Status == ProsumerStatus.Active,
                RegisteredAt = pFallback.RegisteredAt
            };
        }

        throw new NotFoundException("User", userId);
    }

    private static string GetRedirectPath(string role) => role switch
    {
        RoleConstants.Backoffice   => "/backoffice/dashboard",
        RoleConstants.GridOperator => "/grid-operator/dashboard",
        RoleConstants.Prosumer     => "/prosumer/profile",
        _                          => "/"
    };
}
