using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NavioBackend.DTOs
{
    public class ChangePasswordDto
    {
        [JsonPropertyName("currentPassword")]
        public string CurrentPassword { get; set; }
        [JsonPropertyName("newPassword")]
        public string NewPassword { get; set; }
    }
}
