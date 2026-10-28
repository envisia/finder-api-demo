using System.Text.Json.Serialization;

namespace FinderDemo.Models.Additionals;

/// <summary>
/// Definition of an additional ("Zusatzinformation") that is available in a location
/// (<c>ApiAdditionals</c> in the API documentation).
/// </summary>
public sealed class CategoryAdditional
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("identifier")]
    public string? Identifier { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    /// <summary>One of <c>string</c>, <c>decimal</c>, <c>date</c>, <c>select</c>, <c>checkbox</c> (see <see cref="AdditionalValueTypes"/>).</summary>
    [JsonPropertyName("value_type")]
    public string ValueType { get; init; } = AdditionalValueTypes.String;

    /// <summary>Always mandatory.</summary>
    [JsonPropertyName("required")]
    public bool Required { get; init; }

    /// <summary>Mandatory in (one of) the requested locations.</summary>
    [JsonPropertyName("category_required")]
    public bool CategoryRequired { get; init; }

    /// <summary>Suggestions are available via <c>GET /api/v2/additional/suggestion/{id}</c> (type <c>string</c> only).</summary>
    [JsonPropertyName("suggestion")]
    public bool Suggestion { get; init; }

    [JsonPropertyName("in_search_list")]
    public bool InSearchList { get; init; }

    [JsonPropertyName("multiple_values")]
    public bool MultipleValues { get; init; }

    /// <summary>Max value (decimal) or max length (string).</summary>
    [JsonPropertyName("max_value")]
    public double? MaxValue { get; init; }

    /// <summary>Min value (decimal) or min length (string).</summary>
    [JsonPropertyName("min_value")]
    public double? MinValue { get; init; }

    [JsonPropertyName("regex")]
    public string? Regex { get; init; }

    [JsonPropertyName("hidden")]
    public bool Hidden { get; init; }

    public bool IsMandatory => Required || CategoryRequired;
}
