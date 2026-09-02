using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms02.Contracts;

public sealed class ProductMediaDto
{
        [JsonPropertyName("id")]
        [Required]
        public string Id { get; set; }

        [JsonPropertyName("fileName")]
        [Required]
        public string FileName { get; set; }

        [JsonPropertyName("imageType")]
        [Required]
        public string ImageType { get; set; }

        [JsonPropertyName("originalUri")]
        public string? OriginalUri { get; set; }

        [JsonPropertyName("transformedUri")]
        public string? TransformedUri { get; set; }

        [JsonPropertyName("externalUrl")]
        public string? ExternalUrl { get; set; }

        [JsonPropertyName("defaultImage")]
        public bool? DefaultImage { get; set; }

        [JsonPropertyName("mediaStatus")]
        [Required]
        public string MediaStatus { get; set; }
}
