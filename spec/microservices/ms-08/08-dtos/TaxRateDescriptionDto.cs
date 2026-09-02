using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms08.Contracts;

public sealed class TaxRateDescriptionDto
{
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("languageCode")]
        [Required]
        [StringLength(10, MinimumLength = 1)]
        public string LanguageCode { get; set; }

        [JsonPropertyName("name")]
        [Required]
        [StringLength(255, MinimumLength = 1)]
        public string Name { get; set; }

        [JsonPropertyName("title")]
        [MaxLength(255)]
        public string? Title { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
}
