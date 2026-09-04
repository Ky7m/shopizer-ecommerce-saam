using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.MerchantAdministration.DTOs;

public sealed class CreateStoreRequestDto
{
    [JsonPropertyName("code")]
    [Required]
    [StringLength(100, MinimumLength = 1)]
    [RegularExpression(@"^[A-Za-z0-9_]+$")]
    public string Code { get; set; }

    [JsonPropertyName("name")]
    [Required]
    [MinLength(1)]
    public string Name { get; set; }

    [JsonPropertyName("emailAddress")]
    [Required]
    [EmailAddress]
    public string EmailAddress { get; set; }

    [JsonPropertyName("phone")]
    [Required]
    [MinLength(1)]
    public string Phone { get; set; }

    [JsonPropertyName("address")]
    public AddressDto Address { get; set; }

    [JsonPropertyName("parentStoreCode")]
    public string? ParentStoreCode { get; set; }

    [JsonPropertyName("retailer")]
    public bool? Retailer { get; set; }

    [JsonPropertyName("defaultLanguageCode")]
    [Required]
    public string DefaultLanguageCode { get; set; }

    [JsonPropertyName("supportedLanguageCodes")]
    public List<string> SupportedLanguageCodes { get; set; } = new();

    [JsonPropertyName("currencyCode")]
    public string? CurrencyCode { get; set; }

    [JsonPropertyName("dimensionUnit")]
    public string? DimensionUnit { get; set; }

    [JsonPropertyName("weightUnit")]
    public string? WeightUnit { get; set; }
}
