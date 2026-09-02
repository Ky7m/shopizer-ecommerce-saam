using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms10.Contracts;

public sealed class StoreNameListResponseDto
{
        [JsonPropertyName("items")]
        public List<StoreNameDto> Items { get; set; } = new();
}
