using System.Text.Json.Serialization;

namespace FinderDemo.Models.Search;

public sealed class UploadedSearchHit
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("web_url")]
    public string? WebUrl { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("extension")]
    public string? Extension { get; init; }

    // Note: the API really spells this property "archiv_date" (without "e").
    [JsonPropertyName("archiv_date")]
    public DateTimeOffset? ArchiveDate { get; init; }
}
