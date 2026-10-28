using System.Text.Json.Serialization;

namespace FinderDemo.Models.Auth;

/// <summary>OAuth2 error response of the token endpoint, e.g. <c>{"error":"invalid_scope"}</c>.</summary>
internal sealed record TokenErrorResponse
{
    [JsonPropertyName("error")]
    public string? Error { get; init; }

    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; init; }
}
