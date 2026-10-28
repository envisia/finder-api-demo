using System.Text.Json.Serialization;

namespace FinderDemo.Models.Auth;

/// <summary>Successful response of the token endpoint.</summary>
/// <remarks>A class rather than a record so the access token never ends up in a generated <c>ToString</c>.</remarks>
internal sealed class TokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>Lifetime in seconds.</summary>
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; init; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; init; }
}
