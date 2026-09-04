using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.ContentConfiguration.DTOs;

public sealed class ModuleEnvironmentDto
{
    [JsonPropertyName("env")]
    [Required]
    public string Env { get; set; }

    [JsonPropertyName("scheme")]
    public string? Scheme { get; set; }

    [JsonPropertyName("host")]
    public string? Host { get; set; }

    [JsonPropertyName("port")]
    public string? Port { get; set; }

    [JsonPropertyName("uri")]
    public string? Uri { get; set; }

    [JsonPropertyName("config1")]
    public string? Config1 { get; set; }

    [JsonPropertyName("config2")]
    public string? Config2 { get; set; }
}
