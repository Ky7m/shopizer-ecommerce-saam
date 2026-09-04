using System.Text.Json.Serialization;

namespace Shopizer.ContentConfiguration.DTOs;

public sealed class PublicConfigurationDto
{
    [JsonPropertyName("facebook")]
    public string? Facebook { get; set; }

    [JsonPropertyName("pinterest")]
    public string? Pinterest { get; set; }

    [JsonPropertyName("ga")]
    public string? Ga { get; set; }

    [JsonPropertyName("instagram")]
    public string? Instagram { get; set; }

    [JsonPropertyName("allowOnlinePurchase")]
    public bool AllowOnlinePurchase { get; set; }

    [JsonPropertyName("displaySearchBox")]
    public bool DisplaySearchBox { get; set; }

    [JsonPropertyName("displayContactUs")]
    public bool DisplayContactUs { get; set; }

    [JsonPropertyName("displayShipping")]
    public bool DisplayShipping { get; set; }

    [JsonPropertyName("displayCustomerSection")]
    public bool DisplayCustomerSection { get; set; }

    [JsonPropertyName("displayAddToCartOnFeaturedItems")]
    public bool DisplayAddToCartOnFeaturedItems { get; set; }

    [JsonPropertyName("displayCustomerAgreement")]
    public bool DisplayCustomerAgreement { get; set; }

    [JsonPropertyName("displayPagesMenu")]
    public bool DisplayPagesMenu { get; set; }
}
