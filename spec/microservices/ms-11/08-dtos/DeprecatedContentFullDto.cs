using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms11.Contracts;

public sealed class DeprecatedContentFullDto
{
        [JsonPropertyName("id")]
        [Required]
        public string Id { get; set; }

        [JsonPropertyName("code")]
        [Required]
        public string Code { get; set; }

        [JsonPropertyName("contentType")]
        [Required]
        public string ContentType { get; set; }

        [JsonPropertyName("visible")]
        public bool Visible { get; set; }

        [JsonPropertyName("displayedInMenu")]
        public bool DisplayedInMenu { get; set; }

        [JsonPropertyName("descriptions")]
        public List<ContentDescriptionDto> Descriptions { get; set; } = new();
}
