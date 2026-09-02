using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms04.Contracts;

public sealed class AddressDto
{
        [JsonPropertyName("firstName")]
        [Required]
        public string FirstName { get; set; }

        [JsonPropertyName("lastName")]
        [Required]
        public string LastName { get; set; }

        [JsonPropertyName("company")]
        public string? Company { get; set; }

        [JsonPropertyName("address")]
        [Required]
        public string Address { get; set; }

        [JsonPropertyName("city")]
        [Required]
        public string City { get; set; }

        [JsonPropertyName("stateProvince")]
        public string? StateProvince { get; set; }

        [JsonPropertyName("countryCode")]
        [Required]
        public string CountryCode { get; set; }

        [JsonPropertyName("postalCode")]
        [Required]
        public string PostalCode { get; set; }

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }
}
