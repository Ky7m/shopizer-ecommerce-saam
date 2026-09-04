using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.OrderManagement.DTOs;

public sealed class OrderLineAttributeDto
{
    [JsonPropertyName("optionId")]
    public long OptionId { get; set; }

    [JsonPropertyName("optionValueId")]
    public long OptionValueId { get; set; }

    [JsonPropertyName("name")]
    [Required]
    public string Name { get; set; }

    [JsonPropertyName("value")]
    [Required]
    public string Value { get; set; }

    [JsonPropertyName("price")]
    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [JsonPropertyName("free")]
    public bool? Free { get; set; }

    [JsonPropertyName("weight")]
    public decimal? Weight { get; set; }
}
