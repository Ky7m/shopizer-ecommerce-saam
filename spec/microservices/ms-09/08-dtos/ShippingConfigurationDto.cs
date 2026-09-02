using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms09.Contracts;

public sealed class ShippingConfigurationDto
{
        [JsonPropertyName("shippingType")]
        [Required]
        public string ShippingType { get; set; }

        [JsonPropertyName("shippingBasisType")]
        [Required]
        public string ShippingBasisType { get; set; }

        [JsonPropertyName("shippingOptionPriceType")]
        [Required]
        public string ShippingOptionPriceType { get; set; }

        [JsonPropertyName("shippingPackageType")]
        [Required]
        public string ShippingPackageType { get; set; }

        [JsonPropertyName("shippingDescription")]
        public string? ShippingDescription { get; set; }

        [JsonPropertyName("freeShippingType")]
        public string? FreeShippingType { get; set; }

        [JsonPropertyName("boxWidth")]
        public int? BoxWidth { get; set; }

        [JsonPropertyName("boxHeight")]
        public int? BoxHeight { get; set; }

        [JsonPropertyName("boxLength")]
        public int? BoxLength { get; set; }

        [JsonPropertyName("boxWeight")]
        public decimal? BoxWeight { get; set; }

        [JsonPropertyName("maxWeight")]
        public decimal? MaxWeight { get; set; }

        [JsonPropertyName("freeShippingEnabled")]
        public bool FreeShippingEnabled { get; set; }

        [JsonPropertyName("orderTotalFreeShipping")]
        public decimal? OrderTotalFreeShipping { get; set; }

        [JsonPropertyName("handlingFees")]
        public decimal? HandlingFees { get; set; }

        [JsonPropertyName("taxOnShipping")]
        public bool TaxOnShipping { get; set; }

        [JsonPropertyName("packages")]
        public List<ShippingPackageDto>? Packages { get; set; } = new();
}
