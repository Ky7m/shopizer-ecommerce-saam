using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms02.Contracts;

public sealed class ProductDto
{
        [JsonPropertyName("id")]
        [Required]
        public string Id { get; set; }

        [JsonPropertyName("storeId")]
        [Required]
        public string StoreId { get; set; }

        [JsonPropertyName("sku")]
        [Required]
        public string Sku { get; set; }

        [JsonPropertyName("refSku")]
        public string? RefSku { get; set; }

        [JsonPropertyName("status")]
        [Required]
        public string Status { get; set; }

        [JsonPropertyName("visible")]
        public bool Visible { get; set; }

        [JsonPropertyName("available")]
        public bool Available { get; set; }

        [JsonPropertyName("canBePurchased")]
        public bool CanBePurchased { get; set; }

        [JsonPropertyName("dateAvailable")]
        [Required]
        public string DateAvailable { get; set; }

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

        [JsonPropertyName("length")]
        public decimal? Length { get; set; }

        [JsonPropertyName("width")]
        public decimal? Width { get; set; }

        [JsonPropertyName("height")]
        public decimal? Height { get; set; }

        [JsonPropertyName("weight")]
        public decimal? Weight { get; set; }

        [JsonPropertyName("reviewAverage")]
        public decimal? ReviewAverage { get; set; }

        [JsonPropertyName("reviewCount")]
        public int? ReviewCount { get; set; }

        [JsonPropertyName("sortOrder")]
        public int? SortOrder { get; set; }

        [JsonPropertyName("descriptions")]
        public List<ProductDescriptionDto> Descriptions { get; set; } = new();

        [JsonPropertyName("categories")]
        public List<CategoryReferenceDto>? Categories { get; set; } = new();

        [JsonPropertyName("variants")]
        public List<ProductVariantDto>? Variants { get; set; } = new();

        [JsonPropertyName("availabilities")]
        public List<AvailabilityDto> Availabilities { get; set; } = new();

        [JsonPropertyName("options")]
        public List<ProductOptionDto>? Options { get; set; } = new();

        [JsonPropertyName("properties")]
        public List<ProductPropertyDto>? Properties { get; set; } = new();

        [JsonPropertyName("media")]
        public List<ProductMediaDto>? Media { get; set; } = new();

        [JsonPropertyName("price")]
        public PriceDto? Price { get; set; }
}
