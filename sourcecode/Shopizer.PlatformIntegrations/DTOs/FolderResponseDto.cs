using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.PlatformIntegrations.DTOs;

public sealed class FolderResponseDto
{
    [JsonPropertyName("path")]
    [Required]
    public string Path { get; set; }

    [JsonPropertyName("provider")]
    public StorageProviderDto Provider { get; set; }

    [JsonPropertyName("capability")]
    [Required]
    public string Capability { get; set; }

    [JsonPropertyName("status")]
    [Required]
    public string Status { get; set; }
}
