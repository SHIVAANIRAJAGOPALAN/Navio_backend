using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NavioBackend.DTOs
{
    public class AssignAssetsDto
    {
        [JsonPropertyName("driverIds")]
        public List<string>? DriverIds { get; set; }

        [JsonPropertyName("truckIds")]
        public List<string>? TruckIds { get; set; }
    }
}
