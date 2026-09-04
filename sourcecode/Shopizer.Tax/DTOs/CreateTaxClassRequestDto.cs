using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Tax.DTOs;

public sealed class CreateTaxClassRequestDto
{
    [JsonPropertyName("code")]
    [Required]
    [StringLength(10, MinimumLength = 1)]
    public string Code { get; set; }

    [JsonPropertyName("title")]
    [Required]
    [StringLength(32, MinimumLength = 1)]
    public string Title { get; set; }
}
