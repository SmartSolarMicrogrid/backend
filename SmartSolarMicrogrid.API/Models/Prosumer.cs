using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartSolarMicrogrid.API.Models;

public class Prosumer
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    /// <summary>National Identity Card number — primary business identifier.</summary>
    [BsonElement("nic")]
    public string NIC { get; set; } = string.Empty;

    [BsonElement("fullName")]
    public string FullName { get; set; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("phone")]
    public string Phone { get; set; } = string.Empty;

    [BsonElement("address")]
    public string Address { get; set; } = string.Empty;

    [BsonElement("passwordHash")]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Pending | Active | Inactive | DeactivationRequested</summary>
    [BsonElement("status")]
    public string Status { get; set; } = ProsumerStatus.Pending;

    [BsonElement("registeredAt")]
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    [BsonElement("activatedAt")]
    public DateTime? ActivatedAt { get; set; }

    [BsonElement("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
}

public static class ProsumerStatus
{
    public const string Pending               = "Pending";
    public const string Active                = "Active";
    public const string Inactive              = "Inactive";
    public const string DeactivationRequested = "DeactivationRequested";
}
