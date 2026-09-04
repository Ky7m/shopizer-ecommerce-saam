using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.PlatformIntegrations.DTOs;

public sealed class ReplayRequestDto
{
    [JsonPropertyName("reason")]
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string Reason { get; set; }
}
