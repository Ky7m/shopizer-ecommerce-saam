using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.ContentConfiguration.DTOs;

public sealed class ContentFolderResponseDto
{
    [JsonPropertyName("path")]
    [Required]
    public string Path { get; set; }

    [JsonPropertyName("content")]
    public List<ContentFolderEntryDto> Content { get; set; } = new();
}
