using System.Text.Json.Serialization;

namespace Shopizer.CatalogProduct.DTOs;

public sealed class UpdateProductRequestDto
{
        [JsonPropertyName("sku")]
        public string? Sku { get; set; }

        [JsonPropertyName("refSku")]
        public string? RefSku { get; set; }

        [JsonPropertyName("visible")]
        public bool? Visible { get; set; }

        [JsonPropertyName("canBePurchased")]
        public bool? CanBePurchased { get; set; }

        [JsonPropertyName("dateAvailable")]
        public string? DateAvailable { get; set; }

        [JsonPropertyName("manufacturerCode")]
        public string? ManufacturerCode { get; set; }

        [JsonPropertyName("productTypeCode")]
        public string? ProductTypeCode { get; set; }

        [JsonPropertyName("taxClassCode")]
        public string? TaxClassCode { get; set; }

        [JsonPropertyName("productVirtual")]
        public bool? ProductVirtual { get; set; }

        [JsonPropertyName("productShippable")]
        public bool? ProductShippable { get; set; }

        [JsonPropertyName("productFree")]
        public bool? ProductFree { get; set; }

        [JsonPropertyName("sortOrder")]
        public int? SortOrder { get; set; }

        [JsonPropertyName("descriptions")]
        public List<ProductDescriptionDto>? Descriptions { get; set; } = new();

        [JsonPropertyName("availabilities")]
        public List<AvailabilityInputDto>? Availabilities { get; set; } = new();
}
