using System.Text.Json.Serialization;

namespace Shopizer.CartCheckout.DTOs;

public sealed class ListEnvelopeDto
{
    [JsonPropertyName("items")]
    public List<object> Items { get; set; } = new();

    [JsonPropertyName("pagination")]
    public PaginationInfoDto Pagination { get; set; }
}
