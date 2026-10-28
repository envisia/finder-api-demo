using System.Text.Json.Serialization;

namespace FinderDemo.Models.Search;

/// <summary>Response of <c>POST /api/v1/Search/Uploaded</c>.</summary>
public sealed class UploadedSearchResult
{
    [JsonPropertyName("total_hits")]
    public int TotalHits { get; init; }

    [JsonPropertyName("hits")]
    public List<UploadedSearchHit>? Hits { get; init; }
}
