namespace SmartSolarMicrogrid.API.DTOs.Slots;

public class SlotResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public string LocalDate { get; set; } = string.Empty;
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public int Capacity { get; set; }
    public int BookedCount { get; set; }
    public int AvailableCount => Math.Max(0, Capacity - BookedCount);
    public string Status { get; set; } = string.Empty;
    public int Version { get; set; }
}

public class UpdateSlotDto
{
    public int? Capacity { get; set; }
    public string? Status { get; set; } // Available or Blocked
}

public class GenerateSlotsRequestDto
{
    public string FromDate { get; set; } = string.Empty; // YYYY-MM-DD
    public string ToDate { get; set; } = string.Empty;   // YYYY-MM-DD
}
