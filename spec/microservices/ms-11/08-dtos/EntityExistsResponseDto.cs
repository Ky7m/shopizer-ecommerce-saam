using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms11.Contracts;

public sealed class EntityExistsResponseDto
{
        [JsonPropertyName("exists")]
        public bool Exists { get; set; }
}
