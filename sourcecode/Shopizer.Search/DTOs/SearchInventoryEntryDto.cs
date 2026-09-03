using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms03.Contracts;

public sealed class SearchInventoryEntryDto
{
        [JsonPropertyName("sku")]
        [Required]
        public string Sku { get; set; }

        [JsonPropertyName("variantSku")]
        public string? VariantSku { get; set; }

        [JsonPropertyName("quantity")]
        [Range(0, double.MaxValue)]
        public decimal Quantity { get; set; }

        [JsonPropertyName("price")]
        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        [JsonPropertyName("discountedPrice")]
        public decimal? DiscountedPrice { get; set; }

        [JsonPropertyName("optionValues")]
        public Dictionary<string, object?> OptionValues { get; set; } = new();
}
