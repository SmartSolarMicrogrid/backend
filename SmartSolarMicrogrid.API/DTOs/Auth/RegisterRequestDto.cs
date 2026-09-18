namespace SmartSolarMicrogrid.API.DTOs.Auth;

public class RegisterRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    /// <summary>Must be "Backoffice" or "GridOperator".</summary>
    public string Role { get; set; } = string.Empty;
}
