using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.CatalogProduct.DTOs;

public sealed class CategoryReferenceDto
{
        [JsonPropertyName("id")]
        [Required]
        public string Id { get; set; }

        [JsonPropertyName("code")]
        [Required]
        public string Code { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
}
