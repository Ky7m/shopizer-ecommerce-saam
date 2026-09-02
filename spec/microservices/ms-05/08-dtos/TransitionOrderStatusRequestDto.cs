using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms05.Contracts;

public sealed class TransitionOrderStatusRequestDto
{
        [JsonPropertyName("status")]
        public OrderStatusDto Status { get; set; }

        [JsonPropertyName("reason")]
        public string? Reason { get; set; }
}
