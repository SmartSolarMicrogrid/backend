namespace SmartSolarMicrogrid.API.DTOs.Prosumer;

public class CreateProsumerDto
{
    public string NIC { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Status { get; set; }
}
