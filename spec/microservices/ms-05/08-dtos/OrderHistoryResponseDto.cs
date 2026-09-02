using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms05.Contracts;

public sealed class OrderHistoryResponseDto
{
        [JsonPropertyName("items")]
        public List<OrderHistoryEntryDto> Items { get; set; } = new();
}
