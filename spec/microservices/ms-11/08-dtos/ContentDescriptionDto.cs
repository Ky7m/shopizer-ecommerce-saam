using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms11.Contracts;

public sealed class ContentDescriptionDto
{
        [JsonPropertyName("id")]
        [Required]
        public string Id { get; set; }

        [JsonPropertyName("language")]
        [Required]
        [StringLength(10, MinimumLength = 2)]
        public string Language { get; set; }

        [JsonPropertyName("name")]
        [Required]
        [StringLength(120, MinimumLength = 1)]
        public string Name { get; set; }

        [JsonPropertyName("title")]
        [MaxLength(100)]
        public string? Title { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("friendlyUrl")]
        [MaxLength(120)]
        public string? FriendlyUrl { get; set; }

        [JsonPropertyName("metaKeywords")]
        public string? MetaKeywords { get; set; }

        [JsonPropertyName("metaTitle")]
        [MaxLength(100)]
        public string? MetaTitle { get; set; }

        [JsonPropertyName("metaDescription")]
        public string? MetaDescription { get; set; }

        [JsonPropertyName("createdAt")]
        public string? CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public string? UpdatedAt { get; set; }

        [JsonPropertyName("modifiedBy")]
        [MaxLength(60)]
        public string? ModifiedBy { get; set; }
}
