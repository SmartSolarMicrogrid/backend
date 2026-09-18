namespace SmartSolarMicrogrid.API.DTOs.Auth;

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Frontend uses this to navigate to the correct home screen after login.
    /// Backoffice → /backoffice/dashboard
    /// GridOperator → /grid-operator/dashboard
    /// Prosumer → /prosumer/profile
    /// </summary>
    public string RedirectPath { get; set; } = string.Empty;
}
