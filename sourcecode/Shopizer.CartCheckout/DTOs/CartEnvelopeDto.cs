using System.Text.Json.Serialization;

namespace Shopizer.CartCheckout.DTOs;

public sealed class CartEnvelopeDto
{
        [JsonPropertyName("cart")]
        public CartDto Cart { get; set; }
}
