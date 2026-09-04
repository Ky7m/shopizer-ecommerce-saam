using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.CatalogProduct.DTOs;

public sealed class PriceSelectionDto
{
    [JsonPropertyName("optionId")]
    [Required]
    public string OptionId { get; set; }

    [JsonPropertyName("valueId")]
    [Required]
    public string ValueId { get; set; }
}
