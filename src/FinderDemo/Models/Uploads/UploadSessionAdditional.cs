using System.Text.Json.Serialization;
using FinderDemo.Models.Additionals;

namespace FinderDemo.Models.Uploads;

/// <summary>An additional value for an upload session or validation request (<c>id</c> or <c>identifier</c>, plus <c>value</c> or <c>values</c>).</summary>
public sealed class UploadSessionAdditional
{
    [JsonPropertyName("id")]
    public int? Id { get; init; }

    [JsonPropertyName("identifier")]
    public string? Identifier { get; init; }

    /// <summary>Single value (dates as <c>dd.MM.yyyy</c>, decimals with <c>.</c>, checkboxes as <c>true</c>/<c>false</c>).</summary>
    [JsonPropertyName("value")]
    public string? Value { get; init; }

    /// <summary>Multiple values, only for additionals with <see cref="CategoryAdditional.MultipleValues"/>.</summary>
    [JsonPropertyName("values")]
    public IReadOnlyList<string>? Values { get; init; }
}
