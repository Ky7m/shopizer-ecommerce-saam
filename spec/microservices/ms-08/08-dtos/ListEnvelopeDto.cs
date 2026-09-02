using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms08.Contracts;

public sealed class ListEnvelopeDto
{
        [JsonPropertyName("items")]
        public List<object> Items { get; set; } = new();

        [JsonPropertyName("pagination")]
        public PaginationInfoDto Pagination { get; set; }
}
