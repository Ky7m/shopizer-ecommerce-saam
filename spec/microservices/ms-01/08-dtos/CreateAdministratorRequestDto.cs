using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms01.Contracts;

public sealed class CreateAdministratorRequestDto
{
        [JsonPropertyName("userName")]
        [Required]
        public string UserName { get; set; }

        [JsonPropertyName("emailAddress")]
        [Required]
        [EmailAddress]
        public string EmailAddress { get; set; }

        [JsonPropertyName("password")]
        [Required]
        [MinLength(8)]
        public string Password { get; set; }

        [JsonPropertyName("repeatPassword")]
        [Required]
        [MinLength(8)]
        public string RepeatPassword { get; set; }

        [JsonPropertyName("firstName")]
        public string? FirstName { get; set; }

        [JsonPropertyName("lastName")]
        public string? LastName { get; set; }

        [JsonPropertyName("groups")]
        public List<string> Groups { get; set; } = new();

        [JsonPropertyName("defaultLanguageCode")]
        public string? DefaultLanguageCode { get; set; }
}
