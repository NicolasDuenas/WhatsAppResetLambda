using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WhatsAppResetLambda.Models;

public class Company
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    /// <summary>Stored as a string in MongoDB (e.g. "Pro", "Basico", "Free").</summary>
    public string? Plan { get; set; }

    public DateTime DueTo { get; set; }
}
