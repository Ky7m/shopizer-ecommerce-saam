using System.Text.Json.Serialization;

namespace Shopizer.CustomerIdentity.DTOs;

public sealed class EntityExistsResponseDto
{
    [JsonPropertyName("exists")]
    public bool Exists { get; set; }
}
