using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Shipping.DTOs;

public sealed class ShippingPackageRequestDto
{
    [JsonPropertyName("code")]
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Code { get; set; }

    [JsonPropertyName("shippingWidth")]
    [Range(0, double.MaxValue)]
    public decimal ShippingWidth { get; set; }

    [JsonPropertyName("shippingHeight")]
    [Range(0, double.MaxValue)]
    public decimal ShippingHeight { get; set; }

    [JsonPropertyName("shippingLength")]
    [Range(0, double.MaxValue)]
    public decimal ShippingLength { get; set; }

    [JsonPropertyName("shippingWeight")]
    [Range(0, double.MaxValue)]
    public decimal ShippingWeight { get; set; }

    [JsonPropertyName("shippingMaxWeight")]
    [Range(0, double.MaxValue)]
    public decimal ShippingMaxWeight { get; set; }

    [JsonPropertyName("treshold")]
    public int? Treshold { get; set; }

    [JsonPropertyName("type")]
    [Required]
    public string Type { get; set; }

    [JsonPropertyName("defaultPackaging")]
    public bool? DefaultPackaging { get; set; }
}
