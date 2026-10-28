using System.Net.Http.Json;
using FinderDemo.Configuration;
using FinderDemo.Extensions;
using FinderDemo.Logging;
using FinderDemo.Models.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinderDemo.Services;

/// <summary>
/// Gets OAuth2 access tokens from the envisia finder token endpoint (<c>/connect/token</c>) using the
/// client credentials grant, optionally followed by a token exchange to act as a specific user.
/// Tokens are cached per scope until shortly before they expire.
/// </summary>
public sealed class FinderTokenProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<FinderClientOptions> options,
    TimeProvider timeProvider,
    ILogger<FinderTokenProvider> logger)
{
    /// <summary>Default scope, required for search, categories and uploads.</summary>
    public const string ReadWriteScope = "finder.api.read_write";

    /// <summary>Required for <c>GET /api/v2/media/{file}</c> (document download). Must be enabled per app by envisia support.</summary>
    public const string MediaScope = "finder.api.media";

    /// <summary>Allows the app to request short-lived user tokens via token exchange.</summary>
    public const string ImpersonationScope = "finder.impersonation";

    private const string TokenEndpoint = "connect/token";

    private static readonly TimeSpan RefreshMargin = TimeSpan.FromMinutes(1);

    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly Dictionary<string, CachedToken> _cacheByScope = [];

    public async Task<string> GetAccessTokenAsync(string scope, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var current = options.Value;
            var now = timeProvider.GetUtcNow();
            if (_cacheByScope.TryGetValue(scope, out var cached) && cached.IsValid(now, RefreshMargin))
            {
                return cached.AccessToken;
            }

            var token = string.IsNullOrWhiteSpace(current.ImpersonateUpn)
                ? await RequestTokenAsync(current, TokenRequest.ClientCredentials(current, scope), ct).ConfigureAwait(false)
                : await RequestImpersonatedTokenAsync(current, current.ImpersonateUpn, scope, ct).ConfigureAwait(false);

            _cacheByScope[scope] = new CachedToken(token.AccessToken, now.AddSeconds(token.ExpiresIn));
            logger.ReceivedToken(scope, token.ExpiresIn);
            return token.AccessToken;
        }
        finally
        {
            _lock.Release();
        }
    }

    private static async Task<FinderApiException> CreateErrorAsync(HttpResponseMessage response, string scope, CancellationToken ct)
    {
        // OAuth errors are JSON; any other body (e.g. an HTML page of a proxy) is reported by its status code only.
        var error = response.HasMediaType(MediaTypes.Json)
            ? await response.Content.ReadFromJsonAsync<TokenErrorResponse>(FinderJson.Options, ct).ConfigureAwait(false)
            : null;

        var message = error?.Error switch
        {
            "invalid_client" => "The token endpoint rejected the client id / client secret (invalid_client).",
            "invalid_scope" => $"The app is not allowed to request the scope \"{scope}\" (invalid_scope). " +
                               "Additional scopes have to be enabled per app by envisia support.",
            "unauthorized_client" => "The app is not allowed to use this grant type (unauthorized_client).",
            null => $"Token request failed with {(int)response.StatusCode} {response.ReasonPhrase}.",
            _ => $"Token request failed: {error.Error}.",
        };
        if (!string.IsNullOrWhiteSpace(error?.ErrorDescription))
        {
            message += $" {error.ErrorDescription}";
        }

        return new FinderApiException(message, response.StatusCode);
    }

    /// <summary>
    /// Token exchange (RFC 8693): first an "actor" token for the app (scope <c>finder.impersonation</c> plus the
    /// scopes to delegate), then that token is exchanged for a user token (max. 15 minutes lifetime).
    /// </summary>
    private async Task<TokenResponse> RequestImpersonatedTokenAsync(FinderClientOptions current, string upn, string scope, CancellationToken ct)
    {
        var actorRequest = TokenRequest.ClientCredentials(current, $"{ImpersonationScope} {scope}");
        var actor = await RequestTokenAsync(current, actorRequest, ct).ConfigureAwait(false);
        return await RequestTokenAsync(current, TokenRequest.TokenExchange(current, upn, actor.AccessToken, scope), ct).ConfigureAwait(false);
    }

    private async Task<TokenResponse> RequestTokenAsync(FinderClientOptions current, TokenRequest tokenRequest, CancellationToken ct)
    {
        logger.RequestingToken(tokenRequest.GrantType, tokenRequest.Scope);
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(current.BaseUri, TokenEndpoint))
        {
            Content = tokenRequest.ToFormContent(),
        };
        using var response = await httpClientFactory.CreateClient(ServiceCollectionExtensions.HttpClientName)
            .SendAsync(request, ct)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var error = await CreateErrorAsync(response, tokenRequest.Scope, ct).ConfigureAwait(false);
            logger.TokenRequestFailed(tokenRequest.Scope, (int)response.StatusCode, error.Message);
            throw error;
        }

        return await response.Content.ReadFromJsonAsync<TokenResponse>(FinderJson.Options, ct).ConfigureAwait(false)
               ?? throw new FinderApiException("The token endpoint returned an empty response.", response.StatusCode);
    }
}
