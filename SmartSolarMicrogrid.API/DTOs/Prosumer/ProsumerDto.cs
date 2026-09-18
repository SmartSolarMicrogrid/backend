namespace SmartSolarMicrogrid.API.DTOs.Prosumer;

public class ProsumerDto
{
    public string Id { get; set; } = string.Empty;
    public string NIC { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
}
