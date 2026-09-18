using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartSolarMicrogrid.API.Models;

public class EnergyBookingSlot
{
    [BsonId]
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

    [BsonElement("nodeId")]
    public ObjectId NodeId { get; set; }

    [BsonElement("localDate")]
    public string LocalDate { get; set; } = string.Empty;

    [BsonElement("startUtc")]
    public DateTime StartUtc { get; set; }

    [BsonElement("endUtc")]
    public DateTime EndUtc { get; set; }

    [BsonElement("capacity")]
    public int Capacity { get; set; }

    [BsonElement("bookedCount")]
    public int BookedCount { get; set; }

    [BsonElement("status")]
    public SlotStatus Status { get; set; } = SlotStatus.Available;

    [BsonElement("version")]
    public int Version { get; set; } = 1;
}

public enum SlotStatus
{
    Available,
    Blocked,
    Full
}
