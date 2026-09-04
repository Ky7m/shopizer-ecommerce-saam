using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.OrderManagement.DTOs;

public sealed class OrderDto
{
        [JsonPropertyName("orderId")]
        public long OrderId { get; set; }

        [JsonPropertyName("tenantId")]
        [Required]
        public string TenantId { get; set; }

        [JsonPropertyName("storeId")]
        public long StoreId { get; set; }

        [JsonPropertyName("customerId")]
        public long? CustomerId { get; set; }

        [JsonPropertyName("customerEmailAddress")]
        [EmailAddress]
        public string? CustomerEmailAddress { get; set; }

        [JsonPropertyName("status")]
        public OrderStatusDto Status { get; set; }

        [JsonPropertyName("paymentStatus")]
        public PaymentStatusDto? PaymentStatus { get; set; }

        [JsonPropertyName("fulfillmentStatus")]
        public FulfillmentStatusDto? FulfillmentStatus { get; set; }

        [JsonPropertyName("currencyCode")]
        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string CurrencyCode { get; set; }

        [JsonPropertyName("total")]
        [Range(0, double.MaxValue)]
        public decimal Total { get; set; }

        [JsonPropertyName("refundedAmount")]
        public decimal? RefundedAmount { get; set; }

        [JsonPropertyName("refundableAmount")]
        public decimal? RefundableAmount { get; set; }

        [JsonPropertyName("datePurchased")]
        [Required]
        public string DatePurchased { get; set; }

        [JsonPropertyName("orderDateFinished")]
        public string? OrderDateFinished { get; set; }

        [JsonPropertyName("paymentType")]
        public string? PaymentType { get; set; }

        [JsonPropertyName("paymentModuleCode")]
        public string? PaymentModuleCode { get; set; }

        [JsonPropertyName("shippingModuleCode")]
        public string? ShippingModuleCode { get; set; }

        [JsonPropertyName("customerAgreed")]
        public bool? CustomerAgreed { get; set; }

        [JsonPropertyName("confirmedAddress")]
        public bool? ConfirmedAddress { get; set; }

        [JsonPropertyName("locale")]
        public string? Locale { get; set; }

        [JsonPropertyName("billingAddress")]
        public AddressSnapshotDto? BillingAddress { get; set; }

        [JsonPropertyName("deliveryAddress")]
        public AddressSnapshotDto? DeliveryAddress { get; set; }

        [JsonPropertyName("lines")]
        public List<OrderLineDto> Lines { get; set; } = new();

        [JsonPropertyName("totals")]
        public List<OrderTotalDto> Totals { get; set; } = new();

        [JsonPropertyName("attributes")]
        public List<OrderAttributeDto>? Attributes { get; set; } = new();

        [JsonPropertyName("history")]
        public List<OrderHistoryEntryDto>? History { get; set; } = new();

        [JsonPropertyName("downloads")]
        public List<DownloadEntitlementDto>? Downloads { get; set; } = new();
}
