using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.ContentConfiguration.DTOs;

public sealed class RetiredOperationRequestDto
{
    [JsonPropertyName("legacyOperation")]
    [Required]
    public string LegacyOperation { get; set; }
}
