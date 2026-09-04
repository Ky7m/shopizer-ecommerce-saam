using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.PlatformIntegrations.DTOs;

public sealed class UploadFileItemDto
{
    [JsonPropertyName("contentType")]
    public ContentTypeDto ContentType { get; set; }

    [JsonPropertyName("fileName")]
    [Required]
    [MinLength(1)]
    public string FileName { get; set; }

    [JsonPropertyName("mimeType")]
    [Required]
    [MinLength(1)]
    public string MimeType { get; set; }

    [JsonPropertyName("contentBase64")]
    [Required]
    public string ContentBase64 { get; set; }
}
