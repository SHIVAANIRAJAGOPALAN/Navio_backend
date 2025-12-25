using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace NavioBackend.Models
{
    [BsonIgnoreExtraElements]
    public class RoadRestriction
    {
        // MongoDB ObjectId
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        // Road Info
        [JsonPropertyName("road_id")]
        [BsonElement("road_id")]
        public long RoadId { get; set; }

        // Issues on the road
        [JsonPropertyName("issues")]
        [BsonElement("issues")]
        public List<string> Issues { get; set; } = new();

        // Same datetime as DriverNote.dateTime
        [JsonPropertyName("dateTime")]
        [BsonElement("dateTime")]
        public DateTime DateTime { get; set; }
    }
}
