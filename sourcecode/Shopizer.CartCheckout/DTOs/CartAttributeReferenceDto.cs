using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms04.Contracts;

public sealed class CartAttributeReferenceDto
{
        [JsonPropertyName("id")]
        public long Id { get; set; }
}
