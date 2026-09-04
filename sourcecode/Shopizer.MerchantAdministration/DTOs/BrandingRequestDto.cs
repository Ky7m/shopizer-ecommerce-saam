using System.Text.Json.Serialization;

namespace Shopizer.MerchantAdministration.DTOs;

public sealed class BrandingRequestDto
{
    [JsonPropertyName("templateCode")]
    public string? TemplateCode { get; set; }

    [JsonPropertyName("logoUri")]
    public string? LogoUri { get; set; }
}
