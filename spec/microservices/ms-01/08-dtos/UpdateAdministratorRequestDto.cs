using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms01.Contracts;

public sealed class UpdateAdministratorRequestDto
{
        [JsonPropertyName("userName")]
        public string? UserName { get; set; }

        [JsonPropertyName("emailAddress")]
        [EmailAddress]
        public string? EmailAddress { get; set; }

        [JsonPropertyName("firstName")]
        public string? FirstName { get; set; }

        [JsonPropertyName("lastName")]
        public string? LastName { get; set; }

        [JsonPropertyName("groups")]
        public List<string>? Groups { get; set; } = new();

        [JsonPropertyName("storeId")]
        public string? StoreId { get; set; }

        [JsonPropertyName("isActive")]
        public bool? IsActive { get; set; }
}
