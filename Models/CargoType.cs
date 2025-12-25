using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace NavioBackend.Models
{
    public class CargoType
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [JsonPropertyName("id")]
        public string Id { get; set; }

        // The display name, e.g. "Fragile", "Hazardous"
        [JsonPropertyName("name")]
        public string Name { get; set; }

        // Risk level: Low | Medium | High
        [JsonPropertyName("risk")]
        public string Risk { get; set; }

        // A string to drive UI color classes, e.g. "green", "amber", "red"
        [JsonPropertyName("color")]
        public string Color { get; set; }

        // Is this cargo type active?
        [JsonPropertyName("active")]
        public bool Active { get; set; }

        // Optional count metric (routes using this type) - not strictly required in DB
        [JsonPropertyName("count")]
        public int Count { get; set; } = 0;
    }
}

