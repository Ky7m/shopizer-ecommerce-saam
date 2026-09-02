using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms04.Contracts;

public sealed class AddCartItemRequestDto
{
        [JsonPropertyName("product")]
        [Required]
        [MinLength(1)]
        public string Product { get; set; }

        [JsonPropertyName("quantity")]
        [Range(1, double.MaxValue)]
        public int Quantity { get; set; }

        [JsonPropertyName("attributes")]
        public List<CartAttributeReferenceDto>? Attributes { get; set; } = new();

        [JsonPropertyName("promoCode")]
        public string? PromoCode { get; set; }
}
