using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms09.Contracts;

public sealed class ShippingSummaryDto
{
        [JsonPropertyName("shipping")]
        public decimal Shipping { get; set; }

        [JsonPropertyName("handling")]
        public decimal Handling { get; set; }

        [JsonPropertyName("shippingModule")]
        public string? ShippingModule { get; set; }

        [JsonPropertyName("shippingOption")]
        public string? ShippingOption { get; set; }

        [JsonPropertyName("freeShipping")]
        public bool FreeShipping { get; set; }

        [JsonPropertyName("taxOnShipping")]
        public bool TaxOnShipping { get; set; }

        [JsonPropertyName("shippingQuote")]
        public bool? ShippingQuote { get; set; }

        [JsonPropertyName("shippingText")]
        public string? ShippingText { get; set; }

        [JsonPropertyName("handlingText")]
        public string? HandlingText { get; set; }

        [JsonPropertyName("delivery")]
        public DeliveryAddressDto? Delivery { get; set; }

        [JsonPropertyName("selectedShippingOption")]
        public object? SelectedShippingOption { get; set; }

        [JsonPropertyName("shippingOptions")]
        public List<ShippingOptionDto> ShippingOptions { get; set; } = new();

        [JsonPropertyName("quoteInformations")]
        public Dictionary<string, object?>? QuoteInformations { get; set; } = new();
}
