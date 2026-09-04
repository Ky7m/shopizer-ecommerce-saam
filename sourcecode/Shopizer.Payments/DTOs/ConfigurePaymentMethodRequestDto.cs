using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Payments.DTOs;

public sealed class ConfigurePaymentMethodRequestDto
{
    [JsonPropertyName("active")]
    public bool Active { get; set; }

    [JsonPropertyName("defaultSelected")]
    public bool DefaultSelected { get; set; }

    [JsonPropertyName("environment")]
    [Required]
    public string Environment { get; set; }

    [JsonPropertyName("publicConfiguration")]
    public Dictionary<string, object?> PublicConfiguration { get; set; } = new();

    [JsonPropertyName("secretReference")]
    [Required]
    [MinLength(1)]
    public string SecretReference { get; set; }

    [JsonPropertyName("configurationVersion")]
    public long? ConfigurationVersion { get; set; }
}
