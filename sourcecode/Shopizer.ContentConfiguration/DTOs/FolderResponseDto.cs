using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.ContentConfiguration.DTOs;

public sealed class FolderResponseDto
{
    [JsonPropertyName("path")]
    [Required]
    public string Path { get; set; }

    [JsonPropertyName("created")]
    public bool? Created { get; set; }
}
