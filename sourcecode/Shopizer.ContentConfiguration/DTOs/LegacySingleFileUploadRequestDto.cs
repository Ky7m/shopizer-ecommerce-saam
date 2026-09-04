using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.ContentConfiguration.DTOs;

public sealed class LegacySingleFileUploadRequestDto
{
    [JsonPropertyName("file")]
    [Required]
    public string File { get; set; }
}
