using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms10.Contracts;

public sealed class StoreNameDto
{
        [JsonPropertyName("code")]
        [Required]
        public string Code { get; set; }

        [JsonPropertyName("name")]
        [Required]
        public string Name { get; set; }
}
