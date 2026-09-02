using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms02.Contracts;

public sealed class CategoryDescriptionDto
{
        [JsonPropertyName("languageCode")]
        [Required]
        public string LanguageCode { get; set; }

        [JsonPropertyName("name")]
        [Required]
        public string Name { get; set; }

        [JsonPropertyName("friendlyUrl")]
        [Required]
        public string FriendlyUrl { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("metaDescription")]
        public string? MetaDescription { get; set; }
}
