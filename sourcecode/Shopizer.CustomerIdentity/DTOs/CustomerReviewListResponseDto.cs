using System.Text.Json.Serialization;

namespace Shopizer.CustomerIdentity.DTOs;

public sealed class CustomerReviewListResponseDto
{
    [JsonPropertyName("items")]
    public List<CustomerReviewDto> Items { get; set; } = new();

    [JsonPropertyName("pagination")]
    public PaginationInfoDto Pagination { get; set; }
}
