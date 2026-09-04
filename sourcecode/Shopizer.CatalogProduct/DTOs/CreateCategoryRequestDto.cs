using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.CatalogProduct.DTOs;

public sealed class CreateCategoryRequestDto
{
    [JsonPropertyName("code")]
    [Required]
    [StringLength(100, MinimumLength = 1)]
    [RegularExpression(@"^[A-Za-z0-9_-]+$")]
    public string Code { get; set; }

    [JsonPropertyName("parentId")]
    public string? ParentId { get; set; }

    [JsonPropertyName("visible")]
    public bool? Visible { get; set; }

    [JsonPropertyName("featured")]
    public bool? Featured { get; set; }

    [JsonPropertyName("sortOrder")]
    public int? SortOrder { get; set; }

    [JsonPropertyName("descriptions")]
    public List<CategoryDescriptionDto> Descriptions { get; set; } = new();
}
