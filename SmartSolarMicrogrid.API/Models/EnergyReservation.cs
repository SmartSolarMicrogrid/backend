using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SmartSolarMicrogrid.API.Common.Errors;

namespace SmartSolarMicrogrid.API.Models;

public class EnergyReservation
{
    [BsonId]
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

    [BsonElement("reservationNo")]
    public string ReservationNo { get; set; } = string.Empty;

    [BsonElement("prosumerNic")]
    public string ProsumerNic { get; set; } = string.Empty;

    [BsonElement("nodeId")]
    public ObjectId NodeId { get; set; }

    [BsonElement("slotId")]
    public ObjectId SlotId { get; set; }

    [BsonElement("nodeName")]
    public string NodeName { get; set; } = string.Empty;

    [BsonElement("slotStartUtc")]
    public DateTime SlotStartUtc { get; set; }

    [BsonElement("slotEndUtc")]
    public DateTime SlotEndUtc { get; set; }

    [BsonElement("tradeType")]
    public TradeType TradeType { get; set; }

    [BsonElement("requestedKwh")]
    public decimal RequestedKwh { get; set; }

    [BsonElement("unitPrice")]
    public decimal UnitPrice { get; set; }

    [BsonElement("estimatedValue")]
    public decimal EstimatedValue { get; set; }

    [BsonElement("version")]
    public int Version { get; set; } = 1;

    [BsonElement("status")]
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("qr")]
    public QrInfo? Qr { get; set; }

    [BsonElement("transaction")]
    public TransferRecord? Transaction { get; set; }

    [BsonElement("history")]
    public List<StatusChange> History { get; set; } = new();

    public static EnergyReservation Create(
        string prosumerNic,
        SolarStationInfo node,
        EnergyBookingSlot slot,
        TradeType tradeType,
        decimal requestedKwh,
        decimal unitPrice,
        string createdBy,
        DateTime nowUtc)
    {
        var id = ObjectId.GenerateNewId();
        var reservation = new EnergyReservation
        {
            Id = id,
            ReservationNo = $"RSV-{slot.LocalDate.Replace("-", "")}-{id.ToString()[^6..].ToUpperInvariant()}",
            ProsumerNic = prosumerNic,
            NodeId = node.Id,
            SlotId = slot.Id,
            NodeName = node.Name,
            SlotStartUtc = slot.StartUtc,
            SlotEndUtc = slot.EndUtc,
            TradeType = tradeType,
            RequestedKwh = requestedKwh,
            UnitPrice = unitPrice,
            EstimatedValue = Math.Round(requestedKwh * unitPrice, 2, MidpointRounding.AwayFromZero)
        };
        reservation.History.Add(new StatusChange(ReservationStatus.Pending, nowUtc, createdBy, null));
        return reservation;
    }

    public void Approve(string by, DateTime nowUtc, DateTime validFromUtc)
    {
        Require(ReservationStatus.Pending);
        Qr = new QrInfo((Qr?.Version ?? 0) + 1, nowUtc, validFromUtc, SlotEndUtc, null);
        MoveTo(ReservationStatus.Approved, by, nowUtc);
    }

    public void ReturnToPending(string by, DateTime nowUtc)
    {
        Require(ReservationStatus.Pending, ReservationStatus.Approved);
        MoveTo(ReservationStatus.Pending, by, nowUtc, "modified");
    }

    public void Reject(string by, DateTime nowUtc, string reason)
    {
        Require(ReservationStatus.Pending);
        MoveTo(ReservationStatus.Rejected, by, nowUtc, reason);
    }

    public void Cancel(string by, DateTime nowUtc)
    {
        Require(ReservationStatus.Pending, ReservationStatus.Approved);
        MoveTo(ReservationStatus.Cancelled, by, nowUtc);
    }

    public void StartTransfer(string by, DateTime nowUtc)
    {
        Require(ReservationStatus.Approved);
        if (Qr != null)
        {
            Qr.UsedAtUtc = nowUtc;
        }
        MoveTo(ReservationStatus.InProgress, by, nowUtc);
    }

    public void Complete(TransferRecord transfer, string by, DateTime nowUtc)
    {
        Require(ReservationStatus.InProgress);
        Transaction = transfer;
        MoveTo(ReservationStatus.Completed, by, nowUtc);
    }

    public void Expire(DateTime nowUtc)
    {
        Require(ReservationStatus.Pending);
        MoveTo(ReservationStatus.Expired, "system", nowUtc);
    }

    public void MarkNoShow(DateTime nowUtc)
    {
        Require(ReservationStatus.Approved);
        MoveTo(ReservationStatus.NoShow, "system", nowUtc);
    }

    private void Require(params ReservationStatus[] allowed)
    {
        if (!allowed.Contains(Status))
            throw new DomainException(ErrorCodes.InvalidState, $"A {Status} reservation cannot perform this operation.");
    }

    private void MoveTo(ReservationStatus next, string by, DateTime atUtc, string? note = null)
    {
        Status = next;
        IsActive = next is ReservationStatus.Pending or ReservationStatus.Approved or ReservationStatus.InProgress;
        History.Add(new StatusChange(next, atUtc, by, note));
    }
}

public class QrInfo
{
    public int Version { get; set; }
    public DateTime IssuedAtUtc { get; set; }
    public DateTime ValidFromUtc { get; set; }
    public DateTime ValidToUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }

    public QrInfo() { }

    public QrInfo(int version, DateTime issuedAtUtc, DateTime validFromUtc, DateTime validToUtc, DateTime? usedAtUtc)
    {
        Version = version;
        IssuedAtUtc = issuedAtUtc;
        ValidFromUtc = validFromUtc;
        ValidToUtc = validToUtc;
        UsedAtUtc = usedAtUtc;
    }
}

public class StatusChange
{
    public ReservationStatus Status { get; set; }
    public DateTime AtUtc { get; set; }
    public string By { get; set; } = string.Empty;
    public string? Note { get; set; }

    public StatusChange() { }

    public StatusChange(ReservationStatus status, DateTime atUtc, string by, string? note)
    {
        Status = status;
        AtUtc = atUtc;
        By = by;
        Note = note;
    }
}

public class TransferRecord
{
    public decimal MeterStartKwh { get; set; }
    public decimal MeterEndKwh { get; set; }
    public decimal ActualKwh { get; set; }
    public decimal Value { get; set; }
    public string FinalizedBy { get; set; } = string.Empty;
    public DateTime FinalizedAtUtc { get; set; }

    public TransferRecord() { }

    public TransferRecord(decimal meterStartKwh, decimal meterEndKwh, decimal actualKwh, decimal value, string finalizedBy, DateTime finalizedAtUtc)
    {
        MeterStartKwh = meterStartKwh;
        MeterEndKwh = meterEndKwh;
        ActualKwh = actualKwh;
        Value = value;
        FinalizedBy = finalizedBy;
        FinalizedAtUtc = finalizedAtUtc;
    }
}

public enum ReservationStatus
{
    Pending,
    Approved,
    Rejected,
    Cancelled,
    Expired,
    InProgress,
    Completed,
    NoShow
}

public enum TradeType
{
    Export, // Prosumer selling energy to grid
    Import  // Prosumer buying energy from grid
}
