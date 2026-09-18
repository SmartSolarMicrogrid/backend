namespace SmartSolarMicrogrid.API.DTOs.Nodes;

public class CreateNodeDto
{
    public string NodeCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public decimal BuyPricePerKwh { get; set; } = 35.00m;
    public decimal SellPricePerKwh { get; set; } = 45.00m;
    public string OpenTime { get; set; } = "06:00";
    public string CloseTime { get; set; } = "20:00";
    public int SlotDurationMinutes { get; set; } = 60;
    public decimal MaxKwhPerReservation { get; set; } = 50.00m;
    public int CapacityBays { get; set; } = 4;
    public List<string> OperatorIds { get; set; } = new();
}

public class UpdateNodeDto
{
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public decimal BuyPricePerKwh { get; set; }
    public decimal SellPricePerKwh { get; set; }
    public string OpenTime { get; set; } = "06:00";
    public string CloseTime { get; set; } = "20:00";
    public int SlotDurationMinutes { get; set; } = 60;
    public decimal MaxKwhPerReservation { get; set; }
    public int CapacityBays { get; set; }
    public List<string> OperatorIds { get; set; } = new();
}

public class NodeResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string NodeCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public decimal BuyPricePerKwh { get; set; }
    public decimal SellPricePerKwh { get; set; }
    public string OpenTime { get; set; } = string.Empty;
    public string CloseTime { get; set; } = string.Empty;
    public int SlotDurationMinutes { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal MaxKwhPerReservation { get; set; }
    public int CapacityBays { get; set; }
    public List<string> OperatorIds { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}
