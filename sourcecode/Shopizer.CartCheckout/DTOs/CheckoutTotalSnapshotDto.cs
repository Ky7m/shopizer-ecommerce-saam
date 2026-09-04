using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.CartCheckout.DTOs;

public sealed class CheckoutTotalSnapshotDto
{
        [JsonPropertyName("checkoutTotalSnapshotId")]
        [Required]
        public string CheckoutTotalSnapshotId { get; set; }

        [JsonPropertyName("checkoutSessionId")]
        [Required]
        public string CheckoutSessionId { get; set; }

        [JsonPropertyName("currencyCode")]
        [Required]
        public string CurrencyCode { get; set; }

        [JsonPropertyName("subTotal")]
        [Required]
        public string SubTotal { get; set; }

        [JsonPropertyName("discountTotal")]
        [Required]
        public string DiscountTotal { get; set; }

        [JsonPropertyName("shippingTotal")]
        [Required]
        public string ShippingTotal { get; set; }

        [JsonPropertyName("handlingTotal")]
        [Required]
        public string HandlingTotal { get; set; }

        [JsonPropertyName("taxTotal")]
        [Required]
        public string TaxTotal { get; set; }

        [JsonPropertyName("grandTotal")]
        [Required]
        public string GrandTotal { get; set; }

        [JsonPropertyName("inputHash")]
        [Required]
        public string InputHash { get; set; }

        [JsonPropertyName("quotedAt")]
        [Required]
        public string QuotedAt { get; set; }

        [JsonPropertyName("pricingVersion")]
        public string? PricingVersion { get; set; }

        [JsonPropertyName("taxVersion")]
        public string? TaxVersion { get; set; }

        [JsonPropertyName("shippingVersion")]
        public string? ShippingVersion { get; set; }
}
