using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms05.Contracts;

public sealed class OrderLineDto
{
        [JsonPropertyName("orderProductId")]
        public long OrderProductId { get; set; }

        [JsonPropertyName("sku")]
        [Required]
        public string Sku { get; set; }

        [JsonPropertyName("productName")]
        [Required]
        public string ProductName { get; set; }

        [JsonPropertyName("quantity")]
        [Range(1, double.MaxValue)]
        public int Quantity { get; set; }

        [JsonPropertyName("oneTimeCharge")]
        [Range(0, double.MaxValue)]
        public decimal OneTimeCharge { get; set; }

        [JsonPropertyName("attributes")]
        public List<OrderLineAttributeDto>? Attributes { get; set; } = new();

        [JsonPropertyName("prices")]
        public List<OrderLinePriceDto>? Prices { get; set; } = new();
}
