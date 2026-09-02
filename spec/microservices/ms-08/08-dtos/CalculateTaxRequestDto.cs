using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms08.Contracts;

public sealed class CalculateTaxRequestDto
{
        [JsonPropertyName("currencyCode")]
        [Required]
        [RegularExpression(@"^[A-Z]{3}$")]
        public string CurrencyCode { get; set; }

        [JsonPropertyName("customerId")]
        public string? CustomerId { get; set; }

        [JsonPropertyName("orderId")]
        public string? OrderId { get; set; }

        [JsonPropertyName("billingAddress")]
        public AddressSnapshotDto BillingAddress { get; set; }

        [JsonPropertyName("shippingAddress")]
        public object? ShippingAddress { get; set; }

        [JsonPropertyName("items")]
        public List<TaxCalculationItemDto> Items { get; set; } = new();

        [JsonPropertyName("shipping")]
        public object? Shipping { get; set; }

        [JsonPropertyName("languageCode")]
        public string? LanguageCode { get; set; }

        [JsonPropertyName("idempotencyKey")]
        [MaxLength(128)]
        public string? IdempotencyKey { get; set; }
}
