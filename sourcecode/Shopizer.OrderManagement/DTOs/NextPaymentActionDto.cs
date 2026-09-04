using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.OrderManagement.DTOs;

public sealed class NextPaymentActionDto
{
        [JsonPropertyName("orderId")]
        public long OrderId { get; set; }

        [JsonPropertyName("nextAction")]
        [Required]
        public string NextAction { get; set; }

        [JsonPropertyName("lastPaymentAction")]
        public string? LastPaymentAction { get; set; }
}
