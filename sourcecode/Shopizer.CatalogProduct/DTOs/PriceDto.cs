using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms02.Contracts;

public sealed class PriceDto
{
        [JsonPropertyName("id")]
        [Required]
        public string Id { get; set; }

        [JsonPropertyName("amount")]
        [Range(0, double.MaxValue)]
        public decimal Amount { get; set; }

        [JsonPropertyName("currencyCode")]
        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string CurrencyCode { get; set; }

        [JsonPropertyName("priceType")]
        public string? PriceType { get; set; }

        [JsonPropertyName("defaultPrice")]
        public bool DefaultPrice { get; set; }

        [JsonPropertyName("specialAmount")]
        public decimal? SpecialAmount { get; set; }

        [JsonPropertyName("specialStartAt")]
        public string? SpecialStartAt { get; set; }

        [JsonPropertyName("specialEndAt")]
        public string? SpecialEndAt { get; set; }

        [JsonPropertyName("finalAmount")]
        public decimal? FinalAmount { get; set; }

        [JsonPropertyName("discounted")]
        public bool? Discounted { get; set; }
}
