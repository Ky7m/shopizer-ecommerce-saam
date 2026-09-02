using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms12.Contracts;

public sealed class FileListResponseDto
{
        [JsonPropertyName("items")]
        public List<FileAssetDto> Items { get; set; } = new();

        [JsonPropertyName("pagination")]
        public PaginationInfoDto Pagination { get; set; }
}
