using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.CartCheckout.DTOs;

public sealed class CartItemDto
{
        [JsonPropertyName("id")]
        [Required]
        public string Id { get; set; }

        [JsonPropertyName("productId")]
        public long? ProductId { get; set; }

        [JsonPropertyName("sku")]
        [Required]
        public string Sku { get; set; }

        [JsonPropertyName("variantId")]
        public long? VariantId { get; set; }

        [JsonPropertyName("quantity")]
        [Range(1, double.MaxValue)]
        public int Quantity { get; set; }

        [JsonPropertyName("unitPrice")]
        [Required]
        public string UnitPrice { get; set; }

        [JsonPropertyName("subTotal")]
        [Required]
        public string SubTotal { get; set; }

        [JsonPropertyName("obsolete")]
        public bool? Obsolete { get; set; }

        [JsonPropertyName("attributes")]
        public List<CartAttributeReferenceDto> Attributes { get; set; } = new();
}
