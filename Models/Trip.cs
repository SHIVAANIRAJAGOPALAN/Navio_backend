using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NavioBackend.Models
{
    public class Trip
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        // Route Info
        public string PickupLocation { get; set; }
        public string DropLocation { get; set; }
        public DateTime PickupDateTime { get; set; }
        public DateTime ExpectedCompletionTime { get; set; }

        // Cargo
        public string CargoType { get; set; }   // "Oversized, Fragile"
        public double CargoWeight { get; set; }
        public double CargoLength { get; set; }
        public double CargoWidth { get; set; }
        public double CargoHeight { get; set; }

        // Additional
        public string SpecialInstructions { get; set; } = string.Empty;

        // Assignments
        public string FleetManagerId { get; set; }
        public string FleetManagerName { get; set; }
        public string DriverId { get; set; }
        public string DriverName { get; set; }
        public string TruckId { get; set; }
        public string TruckNumber { get; set; }

        // Truck dimensions copied
        public double TruckLength { get; set; }
        public double TruckWidth { get; set; }
        public double TruckHeight { get; set; }
        public double TruckCapacity { get; set; }
        public string CapacityUnit { get; set; }
        public string TruckBodyType { get; set; }
        public string TruckDutyClass { get; set; }

        // Status
        public string Status { get; set; } = "Upcoming";
        public List<string> RoadRestrictions { get; set; } = new List<string>();

        // Progress
        public double DistanceTravelled { get; set; } = 0.0;
        public double Duration { get; set; } = 0.0;

        public string CancellationReason {get; set;} = string.Empty;
    }
}
