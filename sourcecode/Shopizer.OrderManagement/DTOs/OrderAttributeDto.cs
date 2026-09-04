using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.OrderManagement.DTOs;

public sealed class OrderAttributeDto
{
    [JsonPropertyName("identifier")]
    [Required]
    public string Identifier { get; set; }

    [JsonPropertyName("value")]
    [Required]
    public string Value { get; set; }
}
