using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms08.Contracts;

public sealed class TaxClassDto
{
        [JsonPropertyName("id")]
        [Required]
        public string Id { get; set; }

        [JsonPropertyName("tenantId")]
        [Required]
        public string TenantId { get; set; }

        [JsonPropertyName("storeId")]
        [Required]
        public string StoreId { get; set; }

        [JsonPropertyName("code")]
        [Required]
        [StringLength(10, MinimumLength = 1)]
        public string Code { get; set; }

        [JsonPropertyName("title")]
        [Required]
        [StringLength(32, MinimumLength = 1)]
        public string Title { get; set; }
}
