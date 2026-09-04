using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.OrderManagement.DTOs;

public sealed class OrderLinePriceDto
{
    [JsonPropertyName("code")]
    [Required]
    public string Code { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("price")]
    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [JsonPropertyName("specialPrice")]
    public decimal? SpecialPrice { get; set; }

    [JsonPropertyName("specialStartDate")]
    public string? SpecialStartDate { get; set; }

    [JsonPropertyName("specialEndDate")]
    public string? SpecialEndDate { get; set; }

    [JsonPropertyName("defaultPrice")]
    public bool DefaultPrice { get; set; }
}
