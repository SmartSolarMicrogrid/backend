namespace SmartSolarMicrogrid.API.DTOs.Reservations;

public class CreateReservationRequest
{
    public string NodeId { get; set; } = string.Empty;
    public string SlotId { get; set; } = string.Empty;
    public string TradeType { get; set; } = "Export"; // Export or Import
    public decimal RequestedKwh { get; set; }
    public string? ProsumerNic { get; set; } // When Backoffice creates on behalf of a prosumer
}

public class ModifyReservationRequest
{
    public string? NewSlotId { get; set; }
    public decimal? NewRequestedKwh { get; set; }
    public string? NewTradeType { get; set; }
}

public class RejectReservationRequest
{
    public string Reason { get; set; } = "Rejected by operator";
}

public class ReservationResponse
{
    public string Id { get; set; } = string.Empty;
    public string ReservationNo { get; set; } = string.Empty;
    public string ProsumerNic { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public string SlotId { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;
    public DateTime SlotStartUtc { get; set; }
    public DateTime SlotEndUtc { get; set; }
    public string TradeType { get; set; } = string.Empty;
    public decimal RequestedKwh { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal EstimatedValue { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int Version { get; set; }
    public DateTime ChangeDeadlineUtc { get; set; }
    public bool CanModify { get; set; }
    public bool CanCancel { get; set; }
    public int? QrVersion { get; set; }
    public DateTime? QrIssuedAtUtc { get; set; }
    public TransferRecordSummary? Transaction { get; set; }
}

public class TransferRecordSummary
{
    public decimal MeterStartKwh { get; set; }
    public decimal MeterEndKwh { get; set; }
    public decimal ActualKwh { get; set; }
    public decimal Value { get; set; }
    public string FinalizedBy { get; set; } = string.Empty;
    public DateTime FinalizedAtUtc { get; set; }
}

public class ReservationSearchQuery
{
    public string? Status { get; set; }
    public string? NodeId { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public int Limit { get; set; } = 50;
}
