using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms12.Contracts;

public sealed class ReplayRequestDto
{
        [JsonPropertyName("reason")]
        [Required]
        [StringLength(500, MinimumLength = 1)]
        public string Reason { get; set; }
}
