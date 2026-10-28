using System.Text.Json.Serialization;

namespace FinderDemo.Models.Search;

/// <summary>Request body of <c>POST /api/v1/Search/Uploaded</c>.</summary>
public sealed class UploadedSearchRequest
{
    /// <summary>The <c>temporaryName</c> returned when the last upload fragment was accepted.</summary>
    [JsonPropertyName("query")]
    public required string Query { get; init; }
}
