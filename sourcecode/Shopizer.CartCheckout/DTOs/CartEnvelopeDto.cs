using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms04.Contracts;

public sealed class CartEnvelopeDto
{
        [JsonPropertyName("cart")]
        public CartDto Cart { get; set; }
}
