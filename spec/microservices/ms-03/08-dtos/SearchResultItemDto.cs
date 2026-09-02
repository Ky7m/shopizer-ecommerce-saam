using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms03.Contracts;

public sealed class SearchResultItemDto
{
        [JsonPropertyName("productId")]
        public long ProductId { get; set; }

        [JsonPropertyName("locale")]
        [Required]
        [StringLength(16, MinimumLength = 1)]
        public string Locale { get; set; }

        [JsonPropertyName("name")]
        [Required]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("productLink")]
        public string? ProductLink { get; set; }

        [JsonPropertyName("brandName")]
        public string? BrandName { get; set; }

        [JsonPropertyName("categoryName")]
        public string? CategoryName { get; set; }

        [JsonPropertyName("imageUrl")]
        public string? ImageUrl { get; set; }

        [JsonPropertyName("reviewAverage")]
        public decimal? ReviewAverage { get; set; }

        [JsonPropertyName("inventory")]
        public List<SearchInventoryEntryDto> Inventory { get; set; } = new();
}
