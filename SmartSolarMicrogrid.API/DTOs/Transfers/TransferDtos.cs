namespace SmartSolarMicrogrid.API.DTOs.Transfers;

public class QrResponseDto
{
    public string ReservationId { get; set; } = string.Empty;
    public string ReservationNo { get; set; } = string.Empty;
    public int Version { get; set; }
    public string Payload { get; set; } = string.Empty;
    public string BackupCode { get; set; } = string.Empty;
    public DateTime ValidFromUtc { get; set; }
    public DateTime ValidToUtc { get; set; }
}

public class VerifyTransferRequest
{
    public string? Payload { get; set; }
    public string? ReservationId { get; set; }
    public string? BackupCode { get; set; }
}

public class FinalizeTransferRequest
{
    public decimal MeterStartKwh { get; set; }
    public decimal MeterEndKwh { get; set; }
}

public class TransferRecordDto
{
    public string ReservationId { get; set; } = string.Empty;
    public string ReservationNo { get; set; } = string.Empty;
    public string ProsumerNic { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;
    public string TradeType { get; set; } = string.Empty;
    public decimal MeterStartKwh { get; set; }
    public decimal MeterEndKwh { get; set; }
    public decimal ActualKwh { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Value { get; set; }
    public string FinalizedBy { get; set; } = string.Empty;
    public DateTime FinalizedAtUtc { get; set; }
}
