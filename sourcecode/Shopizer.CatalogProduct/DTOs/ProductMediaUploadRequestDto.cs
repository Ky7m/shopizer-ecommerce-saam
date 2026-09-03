using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms02.Contracts;

public sealed class ProductMediaUploadRequestDto
{
        [JsonPropertyName("file")]
        [Required]
        public string File { get; set; }

        [JsonPropertyName("fileName")]
        public string? FileName { get; set; }

        [JsonPropertyName("defaultImage")]
        public bool? DefaultImage { get; set; }
}
