using System.Text.Json.Serialization;

namespace Shopizer.CustomerIdentity.DTOs;

public sealed class AdministratorListResponseDto
{
    [JsonPropertyName("items")]
    public List<AdministratorDto> Items { get; set; } = new();

    [JsonPropertyName("pagination")]
    public PaginationInfoDto Pagination { get; set; }
}
