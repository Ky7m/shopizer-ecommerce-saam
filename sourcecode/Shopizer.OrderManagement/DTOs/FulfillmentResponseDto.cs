using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.OrderManagement.DTOs;

public sealed class FulfillmentResponseDto
{
    [JsonPropertyName("orderId")]
    public long OrderId { get; set; }

    [JsonPropertyName("fulfillmentId")]
    [Required]
    public string FulfillmentId { get; set; }

    [JsonPropertyName("status")]
    public FulfillmentStatusDto Status { get; set; }

    [JsonPropertyName("carrierReference")]
    public string? CarrierReference { get; set; }

    [JsonPropertyName("lastUpdatedAt")]
    public string? LastUpdatedAt { get; set; }
}
