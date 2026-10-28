using System.Text.Json.Serialization;

namespace FinderDemo.Models.Search;

/// <summary>Response of <c>POST /api/v1/Search/Document</c>.</summary>
public sealed class DocumentSearchResult
{
    [JsonPropertyName("total_hits")]
    public long TotalHits { get; init; }

    [JsonPropertyName("hits")]
    public List<DocumentSearchHit>? Hits { get; init; }
}
