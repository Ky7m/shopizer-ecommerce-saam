using System.Text.Json.Serialization;

namespace Shopizer.CartCheckout.DTOs;

public sealed class CartAttributeReferenceDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
}
