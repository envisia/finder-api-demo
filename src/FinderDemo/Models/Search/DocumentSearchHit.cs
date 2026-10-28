using System.Text.Json.Serialization;

namespace FinderDemo.Models.Search;

/// <summary>One document folder in a search result.</summary>
public sealed class DocumentSearchHit
{
    /// <summary>ID (EfinGuid) of the document folder.</summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>ID (EfinGuid) of the newest document inside the folder; use it to download the file.</summary>
    [JsonPropertyName("current_file_id")]
    public string? CurrentFileId { get; init; }

    [JsonPropertyName("web_url")]
    public string? WebUrl { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("extension")]
    public string? Extension { get; init; }

    /// <summary>
    /// Gets the name with extension. Whether <c>name</c> contains the extension depends on the finder system setting
    /// <c>envisia.finder.search.api.remove.folder.extension</c> (off by default: <c>Musterrechnung.pdf</c> + <c>pdf</c>),
    /// so the extension is only appended when it is missing.
    /// </summary>
    public string FileName =>
        string.IsNullOrEmpty(Extension) || (Name?.EndsWith($".{Extension}", StringComparison.OrdinalIgnoreCase) ?? false)
            ? Name ?? Id
            : $"{Name}.{Extension}";

    [JsonPropertyName("archive_date")]
    public DateTimeOffset? ArchiveDate { get; init; }

    [JsonPropertyName("categories")]
    public List<string>? Categories { get; init; }

    [JsonPropertyName("file_number")]
    public List<string>? FileNumbers { get; init; }

    /// <summary>Additionals (title → first value).</summary>
    [JsonPropertyName("additionals")]
    public Dictionary<string, string>? Additionals { get; init; }

    /// <summary>Additionals (title → all values).</summary>
    [JsonPropertyName("multiple_values_additionals")]
    public Dictionary<string, List<string>>? MultipleValuesAdditionals { get; init; }

    /// <summary>Content snippet; only filled when a full text <see cref="DocumentSearchRequest.Query"/> was sent.</summary>
    [JsonPropertyName("content")]
    public string? Content { get; init; }
}
