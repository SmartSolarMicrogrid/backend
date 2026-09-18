using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SmartSolarMicrogrid.API.Auth;

public class JwtTokenGenerator
{
    private readonly JwtSettings _settings;

    public JwtTokenGenerator(JwtSettings settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Generates a signed JWT for a web app user (Backoffice / GridOperator).
    /// </summary>
    public (string token, DateTime expiresAt) GenerateForUser(string userId, string email, string role)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   userId),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.Role,               role),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
        };
        return BuildToken(claims);
    }

    /// <summary>
    /// Generates a signed JWT for a Prosumer — includes NIC claim.
    /// </summary>
    public (string token, DateTime expiresAt) GenerateForProsumer(string prosumerId, string email, string nic)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   prosumerId),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.Role,               RoleConstants.Prosumer),
            new Claim("nic",                         nic),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
        };
        return BuildToken(claims);
    }

    private (string token, DateTime expiresAt) BuildToken(Claim[] claims)
    {
        var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var creds   = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes);

        var token = new JwtSecurityToken(
            issuer:             _settings.Issuer,
            audience:           _settings.Audience,
            claims:             claims,
            expires:            expires,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
