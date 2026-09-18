using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.GeoJsonObjectModel;

namespace SmartSolarMicrogrid.API.Models;

public class SolarStationInfo
{
    [BsonId]
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

    [BsonElement("nodeCode")]
    public string NodeCode { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("location")]
    public GeoJsonPoint<GeoJson2DGeographicCoordinates>? Location { get; set; }

    [BsonElement("latitude")]
    public double Latitude { get; set; }

    [BsonElement("longitude")]
    public double Longitude { get; set; }

    [BsonElement("pricing")]
    public NodePricing Pricing { get; set; } = new();

    [BsonElement("openingHours")]
    public OpeningHours OpeningHours { get; set; } = new();

    [BsonElement("status")]
    public NodeStatus Status { get; set; } = NodeStatus.Active;

    [BsonElement("maxKwhPerReservation")]
    public decimal MaxKwhPerReservation { get; set; } = 50m;

    [BsonElement("capacityBays")]
    public int CapacityBays { get; set; } = 4;

    [BsonElement("operatorIds")]
    public List<string> OperatorIds { get; set; } = new();

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
}

public class NodePricing
{
    [BsonElement("buyPricePerKwh")]
    public decimal BuyPricePerKwh { get; set; } = 35.00m; // Price station pays to prosumer (Export)

    [BsonElement("sellPricePerKwh")]
    public decimal SellPricePerKwh { get; set; } = 45.00m; // Price prosumer pays to station (Import)
}

public class OpeningHours
{
    [BsonElement("openTime")]
    public string OpenTime { get; set; } = "06:00";

    [BsonElement("closeTime")]
    public string CloseTime { get; set; } = "20:00";

    [BsonElement("slotDurationMinutes")]
    public int SlotDurationMinutes { get; set; } = 60;
}

public enum NodeStatus
{
    Active,
    Inactive
}
