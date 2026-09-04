using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.ContentConfiguration.DTOs;

public sealed class ModuleReplacementResponseDto
{
    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("code")]
    [Required]
    public string Code { get; set; }

    [JsonPropertyName("replaced")]
    public bool Replaced { get; set; }

    [JsonPropertyName("cacheInvalidated")]
    public bool CacheInvalidated { get; set; }
}
