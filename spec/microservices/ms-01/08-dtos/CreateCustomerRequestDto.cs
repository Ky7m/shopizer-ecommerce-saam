using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms01.Contracts;

public sealed class CreateCustomerRequestDto
{
        [JsonPropertyName("emailAddress")]
        [Required]
        [EmailAddress]
        public string EmailAddress { get; set; }

        [JsonPropertyName("password")]
        [Required]
        [MinLength(8)]
        public string Password { get; set; }

        [JsonPropertyName("firstName")]
        public string? FirstName { get; set; }

        [JsonPropertyName("lastName")]
        public string? LastName { get; set; }

        [JsonPropertyName("gender")]
        public string? Gender { get; set; }

        [JsonPropertyName("language")]
        public string? Language { get; set; }

        [JsonPropertyName("provider")]
        public string? Provider { get; set; }

        [JsonPropertyName("billing")]
        public AddressDto Billing { get; set; }

        [JsonPropertyName("delivery")]
        public AddressDto? Delivery { get; set; }

        [JsonPropertyName("attributes")]
        public List<CustomerAttributeDto>? Attributes { get; set; } = new();
}
