namespace SmartSolarMicrogrid.API.DTOs.Prosumer;

/// <summary>Prosumer edits their own profile (name, phone, address only).</summary>
public class ProsumerUpdateDto
{
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}
