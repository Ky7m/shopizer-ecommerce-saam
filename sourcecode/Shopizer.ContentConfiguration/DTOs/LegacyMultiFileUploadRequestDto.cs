using System.Text.Json.Serialization;

namespace Shopizer.ContentConfiguration.DTOs;

public sealed class LegacyMultiFileUploadRequestDto
{
    [JsonPropertyName("file")]
    public List<string> File { get; set; } = new();
}
