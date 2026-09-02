using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.CustomerIdentity.DTOs;

public sealed class ResetRequestResponseDto
{
        [JsonPropertyName("status")]
        [Required]
        public string Status { get; set; }
}
