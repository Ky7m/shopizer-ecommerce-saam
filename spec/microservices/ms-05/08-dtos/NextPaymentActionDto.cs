using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms05.Contracts;

public sealed class NextPaymentActionDto
{
        [JsonPropertyName("orderId")]
        public long OrderId { get; set; }

        [JsonPropertyName("nextAction")]
        [Required]
        public string NextAction { get; set; }

        [JsonPropertyName("lastPaymentAction")]
        public string? LastPaymentAction { get; set; }
}
