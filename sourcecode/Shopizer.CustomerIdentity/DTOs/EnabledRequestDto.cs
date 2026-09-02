using System.Text.Json.Serialization;

namespace Shopizer.CustomerIdentity.DTOs;

public sealed class EnabledRequestDto
{
        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }
}
