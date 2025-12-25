using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NavioBackend.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string FullName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }   // "Admin" | "FleetManager" | "Driver"

        public string PasswordHash { get; set; }
        public string Phone { get; set; }
        public string Status { get; set; }
        public string AssignedFleetManagerId { get; set; } // driver → FM

        
        public string Truck { get; set; }
        public string DriverId { get; set; }
        public DateTime LastLogin { get; set; }
        public List<string>? AssignedDriverIds { get; set; }
        public List<string>? AssignedTruckIds { get; set; }

        public int AssignedDriversCount { get; set; } // Computed, not stored
        public int AssignedTrucksCount { get; set; } // Computed, not stored

    }
}



    

