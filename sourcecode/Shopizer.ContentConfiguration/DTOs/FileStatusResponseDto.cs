using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.ContentConfiguration.DTOs;

public sealed class FileStatusResponseDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("error")]
    [Required]
    public string Error { get; set; }

    [JsonPropertyName("preventRetry")]
    public bool PreventRetry { get; set; }
}
