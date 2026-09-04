using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.CartCheckout.DTOs;

public sealed class AnonymousCustomerDto
{
        [JsonPropertyName("email")]
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [JsonPropertyName("firstName")]
        [Required]
        public string FirstName { get; set; }

        [JsonPropertyName("lastName")]
        [Required]
        public string LastName { get; set; }

        [JsonPropertyName("password")]
        public string? Password { get; set; }

        [JsonPropertyName("repeatPassword")]
        public string? RepeatPassword { get; set; }

        [JsonPropertyName("billing")]
        public AddressDto Billing { get; set; }
}
