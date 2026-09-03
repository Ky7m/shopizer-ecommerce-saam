using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.CatalogProduct.DTOs;

public sealed class ProductVariantDto
{
        [JsonPropertyName("id")]
        [Required]
        public string Id { get; set; }

        [JsonPropertyName("productId")]
        [Required]
        public string ProductId { get; set; }

        [JsonPropertyName("sku")]
        [Required]
        public string Sku { get; set; }

        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("status")]
        [Required]
        public string Status { get; set; }

        [JsonPropertyName("defaultSelection")]
        public bool DefaultSelection { get; set; }

        [JsonPropertyName("available")]
        public bool? Available { get; set; }

        [JsonPropertyName("dateAvailable")]
        public string? DateAvailable { get; set; }

        [JsonPropertyName("availability")]
        public List<AvailabilityDto>? Availability { get; set; } = new();

        [JsonPropertyName("price")]
        public PriceDto? Price { get; set; }
}
