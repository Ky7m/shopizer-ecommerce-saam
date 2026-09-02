using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms04.Contracts;

public sealed class ShippingSummaryDto
{
        [JsonPropertyName("quoteId")]
        public string? QuoteId { get; set; }

        [JsonPropertyName("expiresAt")]
        public string? ExpiresAt { get; set; }

        [JsonPropertyName("shippingRequired")]
        public bool ShippingRequired { get; set; }

        [JsonPropertyName("delivery")]
        public AddressDto? Delivery { get; set; }

        [JsonPropertyName("shipping")]
        public string? Shipping { get; set; }

        [JsonPropertyName("handling")]
        public string? Handling { get; set; }

        [JsonPropertyName("shippingModule")]
        public string? ShippingModule { get; set; }

        [JsonPropertyName("shippingOption")]
        public string? ShippingOption { get; set; }

        [JsonPropertyName("shippingOptionCode")]
        public string? ShippingOptionCode { get; set; }

        [JsonPropertyName("freeShipping")]
        public bool? FreeShipping { get; set; }

        [JsonPropertyName("taxOnShipping")]
        public bool? TaxOnShipping { get; set; }

        [JsonPropertyName("options")]
        public List<ShippingOptionDto> Options { get; set; } = new();
}
