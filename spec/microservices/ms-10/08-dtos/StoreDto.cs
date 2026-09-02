using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms10.Contracts;

public sealed class StoreDto
{
        [JsonPropertyName("id")]
        [Required]
        public string Id { get; set; }

        [JsonPropertyName("tenantId")]
        [Required]
        public string TenantId { get; set; }

        [JsonPropertyName("code")]
        [Required]
        public string Code { get; set; }

        [JsonPropertyName("name")]
        [Required]
        public string Name { get; set; }

        [JsonPropertyName("emailAddress")]
        [Required]
        [EmailAddress]
        public string EmailAddress { get; set; }

        [JsonPropertyName("phone")]
        [Required]
        public string Phone { get; set; }

        [JsonPropertyName("address")]
        public AddressDto? Address { get; set; }

        [JsonPropertyName("defaultLanguageCode")]
        [Required]
        public string DefaultLanguageCode { get; set; }

        [JsonPropertyName("supportedLanguageCodes")]
        public List<string>? SupportedLanguageCodes { get; set; } = new();

        [JsonPropertyName("currencyCode")]
        [Required]
        public string CurrencyCode { get; set; }

        [JsonPropertyName("dimensionUnit")]
        [Required]
        public string DimensionUnit { get; set; }

        [JsonPropertyName("weightUnit")]
        [Required]
        public string WeightUnit { get; set; }

        [JsonPropertyName("retailer")]
        public bool Retailer { get; set; }

        [JsonPropertyName("parentStoreCode")]
        public string? ParentStoreCode { get; set; }

        [JsonPropertyName("templateCode")]
        public string? TemplateCode { get; set; }

        [JsonPropertyName("logoUri")]
        public string? LogoUri { get; set; }

        [JsonPropertyName("status")]
        [Required]
        public string Status { get; set; }
}
