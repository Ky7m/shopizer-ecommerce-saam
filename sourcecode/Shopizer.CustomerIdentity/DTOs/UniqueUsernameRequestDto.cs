using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.CustomerIdentity.DTOs;

public sealed class UniqueUsernameRequestDto
{
        [JsonPropertyName("username")]
        [Required]
        public string Username { get; set; }
}
