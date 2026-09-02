using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms05.Contracts;

public sealed class CancelOrderRequestDto
{
        [JsonPropertyName("reason")]
        [Required]
        [MinLength(1)]
        public string Reason { get; set; }
}
