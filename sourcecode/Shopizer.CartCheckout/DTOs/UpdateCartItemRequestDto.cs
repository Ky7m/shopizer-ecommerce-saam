using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.CartCheckout.DTOs;

public sealed class UpdateCartItemRequestDto
{
        [JsonPropertyName("product")]
        [Required]
        public string Product { get; set; }

        [JsonPropertyName("quantity")]
        [Range(0, double.MaxValue)]
        public int Quantity { get; set; }

        [JsonPropertyName("attributes")]
        public List<CartAttributeReferenceDto>? Attributes { get; set; } = new();

        [JsonPropertyName("promoCode")]
        public string? PromoCode { get; set; }
}
