using System.Text.Json.Serialization;

namespace NavioBackend.DTOs
{
    public class CreateActivityLogRequest
    {
        [JsonPropertyName("action")]
        public string Action { get; set; } = null!;

        [JsonPropertyName("category")]
        public string Category { get; set; } = null!;
    }
}
