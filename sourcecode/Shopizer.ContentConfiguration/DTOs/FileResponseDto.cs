using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.ContentConfiguration.DTOs;

public sealed class FileResponseDto
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("fileName")]
    [Required]
    [RegularExpression(@"^(?!.*\.\.)[^/\\]+$")]
    public string FileName { get; set; }

    [JsonPropertyName("mimeType")]
    public string? MimeType { get; set; }

    [JsonPropertyName("contentType")]
    public FileContentTypeDto ContentType { get; set; }

    [JsonPropertyName("path")]
    [Required]
    public string Path { get; set; }

    [JsonPropertyName("provider")]
    public ProviderNameDto Provider { get; set; }

    [JsonPropertyName("state")]
    public FileStateDto State { get; set; }

    [JsonPropertyName("downloadPath")]
    public string? DownloadPath { get; set; }
}
