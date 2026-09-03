using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms04.Contracts;

public sealed class CheckoutSubmissionDto
{
        [JsonPropertyName("submissionId")]
        [Required]
        public string SubmissionId { get; set; }

        [JsonPropertyName("checkoutSessionId")]
        [Required]
        public string CheckoutSessionId { get; set; }

        [JsonPropertyName("tenantId")]
        [Required]
        public string TenantId { get; set; }

        [JsonPropertyName("storeId")]
        [Required]
        public string StoreId { get; set; }

        [JsonPropertyName("currencyCode")]
        [Required]
        public string CurrencyCode { get; set; }

        [JsonPropertyName("amount")]
        [Required]
        public string Amount { get; set; }

        [JsonPropertyName("paymentModule")]
        [Required]
        public string PaymentModule { get; set; }

        [JsonPropertyName("paymentType")]
        [Required]
        public string PaymentType { get; set; }

        [JsonPropertyName("state")]
        [Required]
        public string State { get; set; }

        [JsonPropertyName("orderReference")]
        public string? OrderReference { get; set; }

        [JsonPropertyName("paymentReference")]
        public string? PaymentReference { get; set; }

        [JsonPropertyName("inventoryReservationReference")]
        public string? InventoryReservationReference { get; set; }
}
