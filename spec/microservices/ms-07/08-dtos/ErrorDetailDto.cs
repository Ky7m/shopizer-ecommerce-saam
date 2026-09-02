using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms07.Contracts;

public sealed class ErrorDetailDto
{
        [JsonPropertyName("field")]
        [Required]
        [MinLength(1)]
        public string Field { get; set; }

        [JsonPropertyName("message")]
        [Required]
        [MinLength(1)]
        public string Message { get; set; }
}
