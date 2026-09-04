using System.Text.Json.Serialization;

namespace Shopizer.CustomerIdentity.DTOs;

public sealed class CustomerListResponseDto
{
    [JsonPropertyName("items")]
    public List<CustomerDto> Items { get; set; } = new();

    [JsonPropertyName("pagination")]
    public PaginationInfoDto Pagination { get; set; }
}
