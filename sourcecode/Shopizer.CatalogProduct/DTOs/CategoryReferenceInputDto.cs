using System.Text.Json.Serialization;

namespace Shopizer.CatalogProduct.DTOs;

public sealed class CategoryReferenceInputDto
{
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("code")]
        public string? Code { get; set; }
}
