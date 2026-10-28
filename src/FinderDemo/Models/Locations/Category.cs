using System.Text.Json.Serialization;

namespace FinderDemo.Models.Locations;

/// <summary>A location returned by <c>POST /api/v1/Category/Process</c> or <c>POST /api/v1/Category/Archive</c>.</summary>
public sealed class Category
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>Name of the main process / parent archive category (archive: only set for sub categories).</summary>
    [JsonPropertyName("base_name")]
    public string? BaseName { get; init; }

    /// <summary>Whether "additional words" (keywords) can be added for this location.</summary>
    [JsonPropertyName("additional_words")]
    public bool AdditionalWords { get; init; }

    public string DisplayName => string.IsNullOrWhiteSpace(BaseName) || BaseName == Name ? Name : $"{BaseName} / {Name}";
}
