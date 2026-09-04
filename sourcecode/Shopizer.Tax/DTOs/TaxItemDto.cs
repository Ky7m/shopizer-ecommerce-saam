using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Tax.DTOs;

public sealed class TaxItemDto
{
    [JsonPropertyName("taxCode")]
    [Required]
    public string TaxCode { get; set; }

    [JsonPropertyName("label")]
    [Required]
    public string Label { get; set; }

    [JsonPropertyName("taxClassCode")]
    public string? TaxClassCode { get; set; }

    [JsonPropertyName("taxRatePercent")]
    public decimal TaxRatePercent { get; set; }

    [JsonPropertyName("taxableAmount")]
    public decimal TaxableAmount { get; set; }

    [JsonPropertyName("taxAmount")]
    public decimal TaxAmount { get; set; }

    [JsonPropertyName("piggyback")]
    public bool Piggyback { get; set; }

    [JsonPropertyName("priority")]
    public int Priority { get; set; }
}
