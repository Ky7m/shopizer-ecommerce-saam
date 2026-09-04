using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.CustomerIdentity.DTOs;

public sealed class CustomerDto
{
    [JsonPropertyName("id")]
    [Required]
    public string Id { get; set; }

    [JsonPropertyName("storeId")]
    [Required]
    public string StoreId { get; set; }

    [JsonPropertyName("loginName")]
    [Required]
    public string LoginName { get; set; }

    [JsonPropertyName("emailAddress")]
    [Required]
    [EmailAddress]
    public string EmailAddress { get; set; }

    [JsonPropertyName("gender")]
    public string? Gender { get; set; }

    [JsonPropertyName("dateOfBirth")]
    public string? DateOfBirth { get; set; }

    [JsonPropertyName("companyName")]
    public string? CompanyName { get; set; }

    [JsonPropertyName("provider")]
    public string? Provider { get; set; }

    [JsonPropertyName("status")]
    [Required]
    public string Status { get; set; }

    [JsonPropertyName("defaultLanguageCode")]
    [Required]
    public string DefaultLanguageCode { get; set; }

    [JsonPropertyName("reviewAverage")]
    [Range(0, 5)]
    public decimal ReviewAverage { get; set; }

    [JsonPropertyName("reviewCount")]
    [Range(0, double.MaxValue)]
    public int ReviewCount { get; set; }

    [JsonPropertyName("anonymous")]
    public bool? Anonymous { get; set; }

    [JsonPropertyName("billing")]
    public AddressDto? Billing { get; set; }

    [JsonPropertyName("delivery")]
    public AddressDto? Delivery { get; set; }

    [JsonPropertyName("attributes")]
    public List<CustomerAttributeDto>? Attributes { get; set; } = new();
}
