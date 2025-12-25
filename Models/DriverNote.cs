using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace NavioBackend.Models
{
    [BsonIgnoreExtraElements]
    public class DriverNote
    {
        // MongoDB ObjectId
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        // Trip & Route

        [JsonPropertyName("tripId")]
        [BsonElement("tripId")]
        public string TripId { get; set; } = string.Empty;

        [JsonPropertyName("from")]
        [BsonElement("from")]
        public string From { get; set; } = string.Empty;

        [JsonPropertyName("to")]
        [BsonElement("to")]
        public string To { get; set; } = string.Empty;

        // Driver Info

        [JsonPropertyName("driverId")]
        [BsonElement("driverId")]
        public string DriverId { get; set; } = string.Empty;

        [JsonPropertyName("driverName")]
        [BsonElement("driverName")]
        public string DriverName { get; set; } = string.Empty;

        // Date & Time
        [JsonPropertyName("createdDateTime")]
        [BsonElement("createdDateTime")]
        public DateTime CreatedDateTime { get; set; } = DateTime.UtcNow;

        // If not provided, controller will set this = CreatedDateTime

        [JsonPropertyName("dateTime")]
        [BsonElement("dateTime")]
        public DateTime DateTime { get; set; }

        // Issue Info

        [JsonPropertyName("issueFaced")]
        [BsonElement("issueFaced")]
        public bool IssueFaced { get; set; }

        [JsonPropertyName("road_ids")]
        [BsonElement("road_ids")]
        public List<long> RoadIds { get; set; } = new();

        [JsonPropertyName("road_name")]
        [BsonElement("road_name")]
        public string RoadName { get; set; } = string.Empty;

        [JsonPropertyName("issues")]
        [BsonElement("issues")]
        public List<string> Issues { get; set; } = new();

        // Optional Description (allowed even if issueFaced = false)

        [JsonPropertyName("description")]
        [BsonElement("description")]
        public string Description { get; set; } = string.Empty;

        // Status
        [JsonPropertyName("status")]
        [BsonElement("status")]
        public string Status { get; set; } = "not approved";
    }
}
