using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WhatsAppResetLambda.Models;

public class Doctor
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }

    /// <summary>Stored as a string in MongoDB (e.g. "Receptionist", "Dentist", …).</summary>
    public string? Role { get; set; }

    public int WhatsAppMessagesRemaining { get; set; }
}
