using System.Text.Json.Serialization;

namespace Shopizer.CustomerIdentity.DTOs;

public sealed class ResetTokenValidationResponseDto
{
        [JsonPropertyName("valid")]
        public bool Valid { get; set; }

        [JsonPropertyName("expiresAt")]
        public string? ExpiresAt { get; set; }
}
