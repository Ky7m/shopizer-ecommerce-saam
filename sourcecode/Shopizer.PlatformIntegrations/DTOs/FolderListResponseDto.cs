using System.Text.Json.Serialization;

namespace Shopizer.PlatformIntegrations.DTOs;

public sealed class FolderListResponseDto
{
    [JsonPropertyName("items")]
    public List<string> Items { get; set; } = new();

    [JsonPropertyName("provider")]
    public StorageProviderDto Provider { get; set; }
}
