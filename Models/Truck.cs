using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace NavioBackend.Models
{
    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    public class Truck
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [JsonPropertyName("number")]
        public string TruckNumber { get; set; }   // ex: TRK-2847
        public double Length { get; set; }        // in ft
        public double Width { get; set; }
        public double Height { get; set; }
        [MongoDB.Bson.Serialization.Attributes.BsonElement("CapacityLbs")]
        public int Capacity { get; set; }  
        public string CapacityUnit { get; set; } = "lbs";
        public string Status { get; set; }        // Active | Maintenance | Available
        public string? BodyType { get; set; }
        public string? DutyClass { get; set; }

        public string? AssignedFleetManagerId { get; set; }
        public string? AssignedDriverId { get; set; }
    }
}
