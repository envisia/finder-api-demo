using System.Text.Json.Serialization;

namespace FinderDemo.Models.Errors;

/// <summary>Error body with the media type <c>application/problem+json</c> (RFC 9457, ASP.NET Core validation problems).</summary>
internal sealed class ProblemDetailsResponse
{
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("detail")]
    public string? Detail { get; init; }

    /// <summary>Validation errors: field name → messages.</summary>
    [JsonPropertyName("errors")]
    public Dictionary<string, string[]>? Errors { get; init; }

    public string Describe()
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(Title))
        {
            parts.Add(Title);
        }

        if (!string.IsNullOrWhiteSpace(Detail))
        {
            parts.Add(Detail);
        }

        foreach (var (field, messages) in Errors ?? [])
        {
            parts.Add($"{field}: {string.Join(" ", messages)}");
        }

        return string.Join(" – ", parts);
    }
}
