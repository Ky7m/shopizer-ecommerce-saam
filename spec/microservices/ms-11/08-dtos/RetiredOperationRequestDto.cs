using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms11.Contracts;

public sealed class RetiredOperationRequestDto
{
        [JsonPropertyName("legacyOperation")]
        [Required]
        public string LegacyOperation { get; set; }
}
