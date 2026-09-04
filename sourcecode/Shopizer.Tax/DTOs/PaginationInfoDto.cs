using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Tax.DTOs;

public sealed class PaginationInfoDto
{
    [JsonPropertyName("page")]
    [Range(1, double.MaxValue)]
    public int Page { get; set; }

    [JsonPropertyName("pageSize")]
    [Range(1, double.MaxValue)]
    public int PageSize { get; set; }

    [JsonPropertyName("totalItems")]
    [Range(0, double.MaxValue)]
    public long TotalItems { get; set; }

    [JsonPropertyName("totalPages")]
    [Range(0, double.MaxValue)]
    public int TotalPages { get; set; }
}
