using System.Net.Http.Headers;

namespace FinderDemo.Services;

/// <summary>
/// Adds <c>Authorization: Bearer …</c> to every request of the finder <see cref="HttpClient"/>. The token is for
/// <see cref="FinderTokenProvider.ReadWriteScope"/> unless the request selects another scope with <see cref="Scope"/>,
/// like the download, which needs <see cref="FinderTokenProvider.MediaScope"/>.
/// </summary>
public sealed class FinderAuthorizationHandler(FinderTokenProvider tokens) : DelegatingHandler
{
    /// <summary>Request option that selects the token scope: <c>request.Options.Set(Scope, FinderTokenProvider.MediaScope)</c>.</summary>
    public static readonly HttpRequestOptionsKey<string> Scope = new("envisia-finder.scope");

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var scope = request.Options.TryGetValue(Scope, out var requested) ? requested : FinderTokenProvider.ReadWriteScope;
        var token = await tokens.GetAccessTokenAsync(scope, cancellationToken).ConfigureAwait(false);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
