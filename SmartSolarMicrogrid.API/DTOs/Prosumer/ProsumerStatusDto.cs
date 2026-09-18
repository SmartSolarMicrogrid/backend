namespace SmartSolarMicrogrid.API.DTOs.Prosumer;

public class ProsumerStatusDto
{
    public string Status { get; set; } = string.Empty;
    public DateTime? ActivatedAt { get; set; }
}
