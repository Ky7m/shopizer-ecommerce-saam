using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms01.Contracts;

public sealed class ResetRequestResponseDto
{
        [JsonPropertyName("status")]
        [Required]
        public string Status { get; set; }
}
