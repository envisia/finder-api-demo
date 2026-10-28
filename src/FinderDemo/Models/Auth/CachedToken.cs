namespace FinderDemo.Models.Auth;

/// <summary>An access token and the time it expires.</summary>
/// <remarks>A class rather than a record so the token never ends up in a generated <c>ToString</c>.</remarks>
internal sealed class CachedToken(string accessToken, DateTimeOffset expiresAt)
{
    public string AccessToken { get; } = accessToken;

    public DateTimeOffset ExpiresAt { get; } = expiresAt;

    public bool IsValid(DateTimeOffset now, TimeSpan margin) => ExpiresAt - margin > now;
}
