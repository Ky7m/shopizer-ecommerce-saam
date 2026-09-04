using System.Text.Json.Serialization;

namespace Shopizer.Tax.DTOs;

public sealed class ExistsResponseDto
{
    [JsonPropertyName("exists")]
    public bool Exists { get; set; }
}
