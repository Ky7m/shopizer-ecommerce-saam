using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms02.Contracts;

public sealed class UpdateVariantRequestDto
{
        [JsonPropertyName("sku")]
        public string? Sku { get; set; }

        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("defaultSelection")]
        public bool? DefaultSelection { get; set; }

        [JsonPropertyName("available")]
        public bool? Available { get; set; }

        [JsonPropertyName("dateAvailable")]
        public string? DateAvailable { get; set; }
}
