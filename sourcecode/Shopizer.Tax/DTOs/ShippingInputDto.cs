using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Tax.DTOs;

public sealed class ShippingInputDto
{
    [JsonPropertyName("shippingAmount")]
    [Range(0, double.MaxValue)]
    public decimal ShippingAmount { get; set; }

    [JsonPropertyName("handlingAmount")]
    [Range(0, double.MaxValue)]
    public decimal HandlingAmount { get; set; }
}
