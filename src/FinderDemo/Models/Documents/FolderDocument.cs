using System.Text.Json.Serialization;

namespace FinderDemo.Models.Documents;

/// <summary>A document inside a folder. Its <see cref="EfinGuid"/> is used for downloads.</summary>
public sealed class FolderDocument
{
    [JsonPropertyName("efinGuid")]
    public string EfinGuid { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("pageCount")]
    public int? PageCount { get; init; }

    [JsonPropertyName("pinned")]
    public bool Pinned { get; init; }

    [JsonPropertyName("date")]
    public DateTimeOffset? Date { get; init; }
}
