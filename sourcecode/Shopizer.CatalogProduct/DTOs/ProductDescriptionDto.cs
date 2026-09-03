using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.CatalogProduct.DTOs;

public sealed class ProductDescriptionDto
{
        [JsonPropertyName("languageCode")]
        [Required]
        [MinLength(2)]
        public string LanguageCode { get; set; }

        [JsonPropertyName("name")]
        [Required]
        [MinLength(1)]
        public string Name { get; set; }

        [JsonPropertyName("friendlyUrl")]
        [Required]
        [MinLength(1)]
        public string FriendlyUrl { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("highlights")]
        public string? Highlights { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("keywords")]
        public string? Keywords { get; set; }

        [JsonPropertyName("metaDescription")]
        public string? MetaDescription { get; set; }
}
