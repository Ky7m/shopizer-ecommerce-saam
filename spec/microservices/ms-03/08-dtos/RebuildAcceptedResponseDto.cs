using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms03.Contracts;

public sealed class RebuildAcceptedResponseDto
{
        [JsonPropertyName("rebuildId")]
        [Required]
        public string RebuildId { get; set; }

        [JsonPropertyName("status")]
        public RebuildStatusDto Status { get; set; }

        [JsonPropertyName("accepted")]
        public bool Accepted { get; set; }

        [JsonPropertyName("acceptedAt")]
        [Required]
        public string AcceptedAt { get; set; }
}
