using System.Text.Json.Serialization;

namespace Shopizer.OrderManagement.DTOs;

public sealed class TransitionOrderStatusRequestDto
{
        [JsonPropertyName("status")]
        public OrderStatusDto Status { get; set; }

        [JsonPropertyName("reason")]
        public string? Reason { get; set; }
}
