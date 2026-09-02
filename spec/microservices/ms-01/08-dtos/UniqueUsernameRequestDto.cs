using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms01.Contracts;

public sealed class UniqueUsernameRequestDto
{
        [JsonPropertyName("username")]
        [Required]
        public string Username { get; set; }
}
