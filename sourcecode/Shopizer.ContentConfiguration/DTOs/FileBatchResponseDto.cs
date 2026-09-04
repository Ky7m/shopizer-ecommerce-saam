using System.Text.Json.Serialization;

namespace Shopizer.ContentConfiguration.DTOs;

public sealed class FileBatchResponseDto
{
    [JsonPropertyName("items")]
    public List<FileResponseDto> Items { get; set; } = new();
}
