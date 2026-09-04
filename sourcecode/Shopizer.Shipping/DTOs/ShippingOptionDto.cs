using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Shipping.DTOs;

public sealed class ShippingOptionDto
{
    [JsonPropertyName("optionPrice")]
    public decimal OptionPrice { get; set; }

    [JsonPropertyName("optionPriceText")]
    public string? OptionPriceText { get; set; }

    [JsonPropertyName("optionName")]
    public string? OptionName { get; set; }

    [JsonPropertyName("optionCode")]
    [Required]
    public string OptionCode { get; set; }

    [JsonPropertyName("optionId")]
    [Required]
    public string OptionId { get; set; }

    [JsonPropertyName("optionDeliveryDate")]
    public string? OptionDeliveryDate { get; set; }

    [JsonPropertyName("optionShippingDate")]
    public string? OptionShippingDate { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("shippingModuleCode")]
    [Required]
    public string ShippingModuleCode { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("estimatedNumberOfDays")]
    public int? EstimatedNumberOfDays { get; set; }

    [JsonPropertyName("shippingQuoteOptionId")]
    public string? ShippingQuoteOptionId { get; set; }
}
