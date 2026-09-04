using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.MerchantAdministration.DTOs;

public sealed class BrandingDto
{
    [JsonPropertyName("storeCode")]
    [Required]
    public string StoreCode { get; set; }

    [JsonPropertyName("templateCode")]
    public string? TemplateCode { get; set; }

    [JsonPropertyName("logoUri")]
    public string? LogoUri { get; set; }
}
