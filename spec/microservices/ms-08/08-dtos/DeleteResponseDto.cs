using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms08.Contracts;

public sealed class DeleteResponseDto
{
        [JsonPropertyName("deleted")]
        public bool Deleted { get; set; }

        [JsonPropertyName("id")]
        [Required]
        public string Id { get; set; }
}
