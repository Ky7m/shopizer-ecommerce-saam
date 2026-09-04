using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.ContentConfiguration.DTOs;

public sealed class GenericFileUploadRequestDto
{
    [JsonPropertyName("file")]
    [Required]
    public string File { get; set; }

    [JsonPropertyName("fileName")]
    [RegularExpression(@"^(?!.*\.\.)[^/\\]+$")]
    public string? FileName { get; set; }

    [JsonPropertyName("contentType")]
    public object? ContentType { get; set; }

    [JsonPropertyName("path")]
    [RegularExpression(@"^/$|^(?!.*\.\.)(/[A-Za-z0-9_-]+)+$")]
    public string? Path { get; set; }
}
