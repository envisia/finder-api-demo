using FinderDemo.Configuration;

namespace FinderDemo.Models.Auth;

/// <summary>Form fields of a request to the token endpoint <c>/connect/token</c>.</summary>
/// <remarks>A class rather than a record on purpose: a record's generated <c>ToString</c> would print the client secret.</remarks>
internal sealed class TokenRequest
{
    public required string GrantType { get; init; }

    public required string ClientId { get; init; }

    public required string ClientSecret { get; init; }

    public required string Scope { get; init; }

    public string? SubjectToken { get; init; }

    public string? SubjectTokenType { get; init; }

    public string? ActorToken { get; init; }

    public string? ActorTokenType { get; init; }

    public string? RequestedTokenType { get; init; }

    public static TokenRequest ClientCredentials(FinderClientOptions options, string scope) => new()
    {
        GrantType = "client_credentials",
        ClientId = options.ClientId,
        ClientSecret = options.ClientSecret,
        Scope = scope,
    };

    /// <summary>RFC 8693 token exchange: a user token for <paramref name="upn"/>, proven by the app's own <paramref name="actorToken"/>.</summary>
    public static TokenRequest TokenExchange(FinderClientOptions options, string upn, string actorToken, string scope) => new()
    {
        GrantType = "urn:ietf:params:oauth:grant-type:token-exchange",
        ClientId = options.ClientId,
        ClientSecret = options.ClientSecret,
        Scope = scope,
        SubjectToken = upn,
        SubjectTokenType = "urn:envisia:finder:token-type:upn",
        ActorToken = actorToken,
        ActorTokenType = "urn:ietf:params:oauth:token-type:access_token",
        RequestedTokenType = "urn:ietf:params:oauth:token-type:access_token",
    };

    public FormUrlEncodedContent ToFormContent()
    {
        var fields = new List<KeyValuePair<string, string>>
        {
            new("grant_type", GrantType),
            new("client_id", ClientId),
            new("client_secret", ClientSecret),
            new("scope", Scope),
        };
        AddIfSet(fields, "subject_token", SubjectToken);
        AddIfSet(fields, "subject_token_type", SubjectTokenType);
        AddIfSet(fields, "actor_token", ActorToken);
        AddIfSet(fields, "actor_token_type", ActorTokenType);
        AddIfSet(fields, "requested_token_type", RequestedTokenType);
        return new FormUrlEncodedContent(fields);
    }

    private static void AddIfSet(List<KeyValuePair<string, string>> fields, string name, string? value)
    {
        if (value is not null)
        {
            fields.Add(new(name, value));
        }
    }
}
