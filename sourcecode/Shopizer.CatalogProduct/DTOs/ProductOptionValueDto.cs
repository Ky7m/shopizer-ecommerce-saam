using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.CatalogProduct.DTOs;

public sealed class ProductOptionValueDto
{
    [JsonPropertyName("id")]
    [Required]
    public string Id { get; set; }

    [JsonPropertyName("code")]
    [Required]
    public string Code { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("displayOnly")]
    public bool DisplayOnly { get; set; }

    [JsonPropertyName("priceAdjustment")]
    public decimal? PriceAdjustment { get; set; }

    [JsonPropertyName("imageUri")]
    public string? ImageUri { get; set; }
}
