// FleetManagerCreateDto.cs
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NavioBackend.DTOs
{
    public class FleetManagerCreateDto
    {
        // frontend uses "name" — older code used "FullName"
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("FullName")] public string? FullName { get; set; }

        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("phone")] public string? Phone { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }

        // frontend uses "assignedDrivers" or older "assignedDriverIds"
        [JsonPropertyName("assignedDrivers")] public List<string>? AssignedDrivers { get; set; }
        [JsonPropertyName("assignedDriverIds")] public List<string>? AssignedDriverIds { get; set; }

        // frontend uses "assignedTrucks" or older "assignedTruckIds"
        [JsonPropertyName("assignedTrucks")] public List<string>? AssignedTrucks { get; set; }
        [JsonPropertyName("assignedTruckIds")] public List<string>? AssignedTruckIds { get; set; }
    }
}
