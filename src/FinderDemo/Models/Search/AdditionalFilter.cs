using System.Text.Json.Serialization;

namespace FinderDemo.Models.Search;

/// <summary>An additional ("Zusatzinformation") filter, addressed either by <see cref="Id"/> or <see cref="Identifier"/>.</summary>
public sealed class AdditionalFilter
{
    [JsonPropertyName("id")]
    public int? Id { get; init; }

    [JsonPropertyName("identifier")]
    public string? Identifier { get; init; }

    [JsonPropertyName("value")]
    public string? Value { get; init; }
}
