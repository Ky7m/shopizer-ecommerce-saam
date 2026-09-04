using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.CartCheckout.DTOs;

public sealed class MultiCartItemRequestDto
{
    [JsonPropertyName("product")]
    [Required]
    public string Product { get; set; }

    [JsonPropertyName("quantity")]
    [Range(0, double.MaxValue)]
    public int Quantity { get; set; }

    [JsonPropertyName("attributes")]
    public List<CartAttributeReferenceDto>? Attributes { get; set; } = new();
}
