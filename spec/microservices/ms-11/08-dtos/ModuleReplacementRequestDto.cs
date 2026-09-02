using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms11.Contracts;

public sealed class ModuleReplacementRequestDto
{
        [JsonPropertyName("module")]
        [Required]
        [MinLength(1)]
        public string Module { get; set; }

        [JsonPropertyName("code")]
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Code { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("image")]
        public string? Image { get; set; }

        [JsonPropertyName("customModule")]
        public bool? CustomModule { get; set; }

        [JsonPropertyName("regions")]
        public List<string>? Regions { get; set; } = new();

        [JsonPropertyName("details")]
        public Dictionary<string, object?>? Details { get; set; } = new();

        [JsonPropertyName("configuration")]
        public List<ModuleEnvironmentDto>? Configuration { get; set; } = new();
}
