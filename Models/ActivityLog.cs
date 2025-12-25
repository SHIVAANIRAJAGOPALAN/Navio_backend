using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace NavioBackend.Models
{
    public class ActivityLog
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_id")]
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [BsonElement("timestamp")]
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        // Machine-readable (constant)
        [BsonElement("action")]
        [JsonPropertyName("action")]
        public string Action { get; set; } = null!;

        // Human-readable
        [BsonElement("message")]
        [JsonPropertyName("message")]
        public string Message { get; set; } = null!;

        // Context
        [BsonElement("entityType")]
        [JsonPropertyName("entityType")]
        public string EntityType { get; set; } = null!;

        [BsonElement("entityId")]
        [JsonPropertyName("entityId")]
        public string? EntityId { get; set; }

        // User
        [BsonElement("userId")]
        [JsonPropertyName("userId")]
        public string UserId { get; set; } = null!;

        [BsonElement("userName")]
        [JsonPropertyName("userName")]
        public string UserName { get; set; } = null!;
    }
}
