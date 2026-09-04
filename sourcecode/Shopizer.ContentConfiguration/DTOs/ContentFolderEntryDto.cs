using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.ContentConfiguration.DTOs;

public sealed class ContentFolderEntryDto
{
    [JsonPropertyName("name")]
    [Required]
    public string Name { get; set; }

    [JsonPropertyName("path")]
    [Required]
    public string Path { get; set; }
}
