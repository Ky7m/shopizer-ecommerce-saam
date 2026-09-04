using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.CatalogProduct.DTOs;

public sealed class ExternalMediaRequestDto
{
    [JsonPropertyName("externalUrl")]
    [Required]
    public string ExternalUrl { get; set; }

    [JsonPropertyName("fileName")]
    [Required]
    public string FileName { get; set; }

    [JsonPropertyName("defaultImage")]
    public bool? DefaultImage { get; set; }
}
