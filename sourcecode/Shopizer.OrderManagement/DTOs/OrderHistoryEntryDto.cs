using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.OrderManagement.DTOs;

public sealed class OrderHistoryEntryDto
{
        [JsonPropertyName("historyId")]
        public long HistoryId { get; set; }

        [JsonPropertyName("orderId")]
        public long OrderId { get; set; }

        [JsonPropertyName("status")]
        public OrderStatusDto? Status { get; set; }

        [JsonPropertyName("dateAdded")]
        [Required]
        public string DateAdded { get; set; }

        [JsonPropertyName("comments")]
        public string? Comments { get; set; }

        [JsonPropertyName("actorId")]
        public string? ActorId { get; set; }

        [JsonPropertyName("source")]
        [Required]
        public string Source { get; set; }

        [JsonPropertyName("customerNotified")]
        public bool? CustomerNotified { get; set; }
}
