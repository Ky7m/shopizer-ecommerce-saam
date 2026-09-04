using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.ContentConfiguration.DTOs;

public sealed class ImageFileDto
{
    [JsonPropertyName("url")]
    [Required]
    public string Url { get; set; }

    [JsonPropertyName("name")]
    [Required]
    public string Name { get; set; }

    [JsonPropertyName("size")]
    public long? Size { get; set; }

    [JsonPropertyName("dir")]
    public bool Dir { get; set; }

    [JsonPropertyName("path")]
    [Required]
    public string Path { get; set; }

    [JsonPropertyName("id")]
    [Required]
    public string Id { get; set; }
}
