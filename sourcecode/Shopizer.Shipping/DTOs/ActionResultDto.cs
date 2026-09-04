using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Shipping.DTOs;

public sealed class ActionResultDto
{
    [JsonPropertyName("status")]
    [Required]
    public string Status { get; set; }

    [JsonPropertyName("resourceId")]
    public string? ResourceId { get; set; }
}
