using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms09.Contracts;

public sealed class ActionResultDto
{
        [JsonPropertyName("status")]
        [Required]
        public string Status { get; set; }

        [JsonPropertyName("resourceId")]
        public string? ResourceId { get; set; }
}
