using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms11.Contracts;

public sealed class ContentItemDto
{
        [JsonPropertyName("id")]
        [Required]
        public string Id { get; set; }

        [JsonPropertyName("code")]
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Code { get; set; }

        [JsonPropertyName("contentType")]
        [Required]
        public string ContentType { get; set; }

        [JsonPropertyName("contentPosition")]
        public string? ContentPosition { get; set; }

        [JsonPropertyName("linkToMenu")]
        public bool? LinkToMenu { get; set; }

        [JsonPropertyName("productGroup")]
        public string? ProductGroup { get; set; }

        [JsonPropertyName("sortOrder")]
        public int SortOrder { get; set; }

        [JsonPropertyName("visible")]
        public bool Visible { get; set; }

        [JsonPropertyName("description")]
        public object? Description { get; set; }

        [JsonPropertyName("descriptions")]
        public List<ContentDescriptionDto>? Descriptions { get; set; } = new();

        [JsonPropertyName("tenantId")]
        public string? TenantId { get; set; }

        [JsonPropertyName("storeId")]
        public string? StoreId { get; set; }

        [JsonPropertyName("createdAt")]
        public string? CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public string? UpdatedAt { get; set; }

        [JsonPropertyName("modifiedBy")]
        [MaxLength(60)]
        public string? ModifiedBy { get; set; }
}
