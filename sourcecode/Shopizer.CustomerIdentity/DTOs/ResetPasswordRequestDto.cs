using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.CustomerIdentity.DTOs;

public sealed class ResetPasswordRequestDto
{
        [JsonPropertyName("password")]
        [Required]
        [MinLength(8)]
        public string Password { get; set; }

        [JsonPropertyName("repeatPassword")]
        [Required]
        [MinLength(8)]
        public string RepeatPassword { get; set; }
}
