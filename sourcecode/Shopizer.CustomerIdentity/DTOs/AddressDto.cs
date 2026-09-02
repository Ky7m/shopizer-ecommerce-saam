using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.CustomerIdentity.DTOs;

public sealed class AddressDto
{
        [JsonPropertyName("addressType")]
        [Required]
        public string AddressType { get; set; }

        [JsonPropertyName("firstName")]
        [Required]
        public string FirstName { get; set; }

        [JsonPropertyName("lastName")]
        [Required]
        public string LastName { get; set; }

        [JsonPropertyName("companyName")]
        public string? CompanyName { get; set; }

        [JsonPropertyName("streetAddress")]
        [Required]
        public string StreetAddress { get; set; }

        [JsonPropertyName("city")]
        [Required]
        public string City { get; set; }

        [JsonPropertyName("postalCode")]
        [Required]
        public string PostalCode { get; set; }

        [JsonPropertyName("stateProvince")]
        public string? StateProvince { get; set; }

        [JsonPropertyName("telephone")]
        public string? Telephone { get; set; }

        [JsonPropertyName("countryCode")]
        [Required]
        public string CountryCode { get; set; }

        [JsonPropertyName("zoneCode")]
        public string? ZoneCode { get; set; }

        [JsonPropertyName("latitude")]
        public string? Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public string? Longitude { get; set; }
}
