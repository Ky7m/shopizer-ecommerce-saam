using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms11.Contracts;

public sealed class ContentDescriptionInputDto
{
        [JsonPropertyName("language")]
        [Required]
        [StringLength(10, MinimumLength = 2)]
        [RegularExpression(@"^[A-Za-z]{2,10}$")]
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

        [JsonPropertyName("metaDescription")]
        public string? MetaDescription { get; set; }
}
