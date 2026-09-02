using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms01.Contracts;

public sealed class AdministratorDto
{
        [JsonPropertyName("id")]
        [Required]
        public string Id { get; set; }

        [JsonPropertyName("storeId")]
        [Required]
        public string StoreId { get; set; }

        [JsonPropertyName("userName")]
        [Required]
        public string UserName { get; set; }

        [JsonPropertyName("emailAddress")]
        [Required]
        [EmailAddress]
        public string EmailAddress { get; set; }

        [JsonPropertyName("firstName")]
        public string? FirstName { get; set; }

        [JsonPropertyName("lastName")]
        public string? LastName { get; set; }

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }

        [JsonPropertyName("defaultLanguageCode")]
        public string? DefaultLanguageCode { get; set; }

        [JsonPropertyName("groups")]
        public List<string> Groups { get; set; } = new();

        [JsonPropertyName("permissions")]
        public List<string>? Permissions { get; set; } = new();
}
