using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms11.Contracts;

public sealed class CreateBoxRequestDto
{
        [JsonPropertyName("code")]
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Code { get; set; }

        [JsonPropertyName("visible")]
        public bool Visible { get; set; }

        [JsonPropertyName("sortOrder")]
        public int? SortOrder { get; set; }

        [JsonPropertyName("contentPosition")]
        public string? ContentPosition { get; set; }

        [JsonPropertyName("productGroup")]
        public string? ProductGroup { get; set; }

        [JsonPropertyName("descriptions")]
        public List<ContentDescriptionInputDto> Descriptions { get; set; } = new();
}
