using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NavioBackend.DTOs
{
    public class TransferAssetsDto
    {
        [JsonPropertyName("sourceManagerId")]
        public string SourceManagerId { get; set; }

        [JsonPropertyName("targetManagerId")]
        public string TargetManagerId { get; set; }

        [JsonPropertyName("drivers")]
        public List<string>? Drivers { get; set; }

        [JsonPropertyName("trucks")]
        public List<string>? Trucks { get; set; }
    }
}