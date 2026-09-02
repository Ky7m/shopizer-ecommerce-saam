using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms05.Contracts;

public sealed class AppendHistoryRequestDto
{
        [JsonPropertyName("status")]
        public OrderStatusDto Status { get; set; }

        [JsonPropertyName("comments")]
        public string? Comments { get; set; }

        [JsonPropertyName("source")]
        [Required]
        public string Source { get; set; }
}
