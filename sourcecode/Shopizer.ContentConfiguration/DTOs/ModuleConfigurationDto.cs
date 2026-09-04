using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.ContentConfiguration.DTOs;

public sealed class ModuleConfigurationDto
{
    [JsonPropertyName("moduleConfigurationId")]
    [Required]
    public string ModuleConfigurationId { get; set; }

    [JsonPropertyName("moduleFamily")]
    [Required]
    public string ModuleFamily { get; set; }

    [JsonPropertyName("code")]
    [Required]
    public string Code { get; set; }

    [JsonPropertyName("regions")]
    public List<string>? Regions { get; set; } = new();

    [JsonPropertyName("configuration")]
    public List<ModuleEnvironmentDto>? Configuration { get; set; } = new();

    [JsonPropertyName("details")]
    public Dictionary<string, object?>? Details { get; set; } = new();

    [JsonPropertyName("moduleType")]
    public string? ModuleType { get; set; }

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("customModule")]
    public bool CustomModule { get; set; }

    [JsonPropertyName("createdAt")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public string? UpdatedAt { get; set; }

    [JsonPropertyName("modifiedBy")]
    public string? ModifiedBy { get; set; }
}
