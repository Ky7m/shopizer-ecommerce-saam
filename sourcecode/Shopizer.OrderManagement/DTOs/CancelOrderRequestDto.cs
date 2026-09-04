using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.OrderManagement.DTOs;

public sealed class CancelOrderRequestDto
{
        [JsonPropertyName("reason")]
        [Required]
        [MinLength(1)]
        public string Reason { get; set; }
}
