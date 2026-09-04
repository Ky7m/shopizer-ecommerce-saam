using System.Text.Json.Serialization;

namespace Shopizer.ContentConfiguration.DTOs;

public sealed class MerchantConfigurationPayloadDto
{
    [JsonPropertyName("displayCustomerSection")]
    public bool DisplayCustomerSection { get; set; }

    [JsonPropertyName("displayContactUs")]
    public bool? DisplayContactUs { get; set; }

    [JsonPropertyName("displayStoreAddress")]
    public bool? DisplayStoreAddress { get; set; }

    [JsonPropertyName("displayAddToCartOnFeaturedItems")]
    public bool? DisplayAddToCartOnFeaturedItems { get; set; }

    [JsonPropertyName("displayCustomerAgreement")]
    public bool? DisplayCustomerAgreement { get; set; }

    [JsonPropertyName("displayPagesMenu")]
    public bool? DisplayPagesMenu { get; set; }

    [JsonPropertyName("allowPurchaseItems")]
    public bool? AllowPurchaseItems { get; set; }

    [JsonPropertyName("displaySearchBox")]
    public bool? DisplaySearchBox { get; set; }

    [JsonPropertyName("testMode")]
    public bool? TestMode { get; set; }

    [JsonPropertyName("debugMode")]
    public bool? DebugMode { get; set; }

    [JsonPropertyName("useDefaultSearchConfig")]
    public Dictionary<string, object?>? UseDefaultSearchConfig { get; set; } = new();

    [JsonPropertyName("defaultSearchConfigPath")]
    public Dictionary<string, object?>? DefaultSearchConfigPath { get; set; } = new();

    [JsonPropertyName("socialValues")]
    public Dictionary<string, object?>? SocialValues { get; set; } = new();
}
