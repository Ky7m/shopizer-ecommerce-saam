using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms02.Contracts;

public sealed class CreateProductRequestDto
{
        [JsonPropertyName("sku")]
        [Required]
        [MinLength(1)]
        [RegularExpression(@"^[A-Za-z0-9]+([_-][A-Za-z0-9]+)*$")]
        public string Sku { get; set; }

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
        public List<ProductDescriptionDto> Descriptions { get; set; } = new();

        [JsonPropertyName("availabilities")]
        public List<AvailabilityInputDto> Availabilities { get; set; } = new();

        [JsonPropertyName("categories")]
        public List<CategoryReferenceInputDto>? Categories { get; set; } = new();

        [JsonPropertyName("variants")]
        public List<CreateVariantRequestDto>? Variants { get; set; } = new();
}
