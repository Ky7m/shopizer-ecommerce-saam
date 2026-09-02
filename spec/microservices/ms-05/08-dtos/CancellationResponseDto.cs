using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms05.Contracts;

public sealed class CancellationResponseDto
{
        [JsonPropertyName("orderId")]
        public long OrderId { get; set; }

        [JsonPropertyName("status")]
        public OrderStatusDto Status { get; set; }

        [JsonPropertyName("compensationState")]
        [Required]
        public string CompensationState { get; set; }
}
