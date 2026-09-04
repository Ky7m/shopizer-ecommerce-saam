using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.MerchantAdministration.DTOs;

public sealed class StoreNameDto
{
    [JsonPropertyName("code")]
    [Required]
    public string Code { get; set; }

    [JsonPropertyName("name")]
    [Required]
    public string Name { get; set; }
}
