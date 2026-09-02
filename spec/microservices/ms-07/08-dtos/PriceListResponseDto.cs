using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms07.Contracts;

public sealed class PriceListResponseDto
{
        [JsonPropertyName("items")]
        public List<PriceDto> Items { get; set; } = new();
}
