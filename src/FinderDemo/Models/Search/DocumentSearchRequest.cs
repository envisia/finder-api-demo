using System.Text.Json.Serialization;

namespace FinderDemo.Models.Search;

/// <summary>
/// Request body of <c>POST /api/v1/Search/Document</c> (<c>DocumentSearchApiContainer</c>).
/// All filters are optional and combined with AND. Properties left <c>null</c> are not sent.
/// </summary>
public sealed record DocumentSearchRequest
{
    /// <summary>Location IDs (process steps / archive categories) to search in. <c>null</c> = all accessible locations.</summary>
    [JsonPropertyName("categories")]
    public IReadOnlyList<int>? Categories { get; init; }

    /// <summary>Full text query over the content of the documents in the folder.</summary>
    [JsonPropertyName("query")]
    public string? Query { get; init; }

    /// <summary>Name of the document folder ("Dokumenten Mappe").</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>ID ("EfinGuid") of the document folder, e.g. <c>efin_04052016_bf5d216e1bb64a03a37ebcd1fe262cc3</c>.</summary>
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    /// <summary>File numbers ("Aktenzeichen").</summary>
    [JsonPropertyName("file_number")]
    public IReadOnlyList<string>? FileNumbers { get; init; }

    /// <summary>Additionals that must be present with the given value (by ID or identifier).</summary>
    [JsonPropertyName("additionals")]
    public IReadOnlyList<AdditionalFilter>? Additionals { get; init; }

    /// <summary>Additionals that must <b>not</b> have the given value (by ID or identifier).</summary>
    [JsonPropertyName("exclude_additionals")]
    public IReadOnlyList<AdditionalFilter>? ExcludeAdditionals { get; init; }

    /// <summary>
    /// IDs of additionals that must <b>not be set</b> on the folder, i.e. "search for folders where additional X is empty".
    /// Only numeric IDs are accepted here (no identifiers).
    /// </summary>
    [JsonPropertyName("empty_additionals")]
    public IReadOnlyList<int>? EmptyAdditionals { get; init; }

    /// <summary>Paging: zero based offset into the full hit list.</summary>
    [JsonPropertyName("from_hit")]
    public int FromHit { get; init; }

    /// <summary>Paging: number of folders to return.</summary>
    [JsonPropertyName("hits_size")]
    public int HitsSize { get; init; } = 25;
}
