using System.Net.Http.Headers;
using FinderDemo.Services;

namespace FinderDemo.Extensions;

internal static class HttpClientAuthExtensions
{
    /// <summary>
    /// Attaches <c>Authorization: Bearer …</c> for the requested scope and sends the request.
    /// The caller owns the request; a failed response is disposed and turned into a <see cref="FinderApiException"/>.
    /// </summary>
    public static async Task<HttpResponseMessage> SendAuthorizedAsync(
        this HttpClient http,
        FinderTokenProvider tokens,
        HttpRequestMessage request,
        string scope,
        HttpCompletionOption completion,
        bool ensureSuccess,
        CancellationToken ct)
    {
        var token = await tokens.GetAccessTokenAsync(scope, ct).ConfigureAwait(false);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await http.SendAsync(request, completion, ct).ConfigureAwait(false);
        if (!ensureSuccess || response.IsSuccessStatusCode)
        {
            return response;
        }

        using var failed = response;
        throw await FinderApiException.FromResponseAsync(failed, ct).ConfigureAwait(false);
    }
}
