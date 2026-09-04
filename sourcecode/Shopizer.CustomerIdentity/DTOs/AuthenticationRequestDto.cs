using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.CustomerIdentity.DTOs;

public sealed class AuthenticationRequestDto
{
    [JsonPropertyName("username")]
    [Required]
    public string Username { get; set; }

    [JsonPropertyName("password")]
    [Required]
    public string Password { get; set; }
}
