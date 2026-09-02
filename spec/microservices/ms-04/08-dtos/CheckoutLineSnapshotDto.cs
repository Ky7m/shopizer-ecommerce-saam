using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms04.Contracts;

public sealed class CheckoutLineSnapshotDto
{
        [JsonPropertyName("checkoutLineSnapshotId")]
        [Required]
        public string CheckoutLineSnapshotId { get; set; }

        [JsonPropertyName("checkoutSessionId")]
        [Required]
        public string CheckoutSessionId { get; set; }

        [JsonPropertyName("lineNumber")]
        public int LineNumber { get; set; }

        [JsonPropertyName("sku")]
        [Required]
        public string Sku { get; set; }

        [JsonPropertyName("productName")]
        [Required]
        public string ProductName { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("unitPrice")]
        [Required]
        public string UnitPrice { get; set; }

        [JsonPropertyName("lineSubTotal")]
        [Required]
        public string LineSubTotal { get; set; }

        [JsonPropertyName("productId")]
        public long ProductId { get; set; }

        [JsonPropertyName("variantId")]
        public long? VariantId { get; set; }

        [JsonPropertyName("isVirtual")]
        public bool? IsVirtual { get; set; }

        [JsonPropertyName("attributes")]
        public List<Dictionary<string, object?>> Attributes { get; set; } = new();
}
