using System.Text.Json.Serialization;

namespace FinderDemo.Models.Locations;

/// <summary>Request body of <c>POST /api/v1/Category/Additionals</c>.</summary>
public sealed class CategoryAdditionalsRequest
{
    /// <summary>Location IDs.</summary>
    [JsonPropertyName("id")]
    public required IReadOnlyList<int> Id { get; init; }
}
