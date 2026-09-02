using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms05.Contracts;

public sealed class DownloadEntitlementDto
{
        [JsonPropertyName("downloadId")]
        public long DownloadId { get; set; }

        [JsonPropertyName("orderId")]
        public long OrderId { get; set; }

        [JsonPropertyName("productName")]
        [Required]
        public string ProductName { get; set; }

        [JsonPropertyName("fileName")]
        [Required]
        public string FileName { get; set; }

        [JsonPropertyName("downloadCount")]
        [Range(0, double.MaxValue)]
        public int DownloadCount { get; set; }

        [JsonPropertyName("downloadExpiryDays")]
        [Range(1, double.MaxValue)]
        public int DownloadExpiryDays { get; set; }

        [JsonPropertyName("accessState")]
        [Required]
        public string AccessState { get; set; }

        [JsonPropertyName("expiresAt")]
        public string? ExpiresAt { get; set; }
}
