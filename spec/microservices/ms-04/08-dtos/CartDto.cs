using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms04.Contracts;

public sealed class CartDto
{
        [JsonPropertyName("id")]
        [Required]
        public string Id { get; set; }

        [JsonPropertyName("code")]
        [Required]
        public string Code { get; set; }

        [JsonPropertyName("tenantId")]
        public string? TenantId { get; set; }

        [JsonPropertyName("storeId")]
        public string? StoreId { get; set; }

        [JsonPropertyName("customerId")]
        public string? CustomerId { get; set; }

        [JsonPropertyName("status")]
        [Required]
        public string Status { get; set; }

        [JsonPropertyName("submittedOrderId")]
        public string? SubmittedOrderId { get; set; }

        [JsonPropertyName("promoCode")]
        public string? PromoCode { get; set; }

        [JsonPropertyName("promoAddedAt")]
        public string? PromoAddedAt { get; set; }

        [JsonPropertyName("items")]
        public List<CartItemDto> Items { get; set; } = new();

        [JsonPropertyName("subTotal")]
        public string? SubTotal { get; set; }

        [JsonPropertyName("total")]
        public string? Total { get; set; }

        [JsonPropertyName("currency")]
        [Required]
        [RegularExpression(@"^[A-Z]{3}$")]
        public string Currency { get; set; }
}
