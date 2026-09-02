using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms11.Contracts;

public sealed class FileListResponseDto
{
        [JsonPropertyName("items")]
        public List<FileResponseDto> Items { get; set; } = new();

        [JsonPropertyName("page")]
        [Range(0, double.MaxValue)]
        public int Page { get; set; }

        [JsonPropertyName("count")]
        [Range(1, double.MaxValue)]
        public int Count { get; set; }

        [JsonPropertyName("number")]
        [Range(0, double.MaxValue)]
        public int Number { get; set; }

        [JsonPropertyName("totalPages")]
        [Range(0, double.MaxValue)]
        public int TotalPages { get; set; }

        [JsonPropertyName("recordsTotal")]
        [Range(0, double.MaxValue)]
        public long RecordsTotal { get; set; }

        [JsonPropertyName("recordsFiltered")]
        [Range(0, double.MaxValue)]
        public long RecordsFiltered { get; set; }

        [JsonPropertyName("pagination")]
        public PaginationInfoDto? Pagination { get; set; }
}
