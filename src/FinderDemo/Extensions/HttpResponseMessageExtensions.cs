using System.Net.Http.Json;
using FinderDemo.Services;

namespace FinderDemo.Extensions;

internal static class HttpResponseMessageExtensions
{
    /// <summary>
    /// Whether the response declares the given media type. Used to decide up front how to read an error body,
    /// instead of trying to deserialize it and catching the failure.
    /// </summary>
    public static bool HasMediaType(this HttpResponseMessage response, string mediaType) =>
        string.Equals(response.Content.Headers.ContentType?.MediaType, mediaType, StringComparison.OrdinalIgnoreCase);

    /// <summary>Reads the JSON body into <typeparamref name="T"/> with the API's serializer settings.</summary>
    public static async Task<T> ReadJsonAsync<T>(this HttpResponseMessage response, CancellationToken ct) =>
        await response.Content.ReadFromJsonAsync<T>(FinderJson.Options, ct).ConfigureAwait(false)
        ?? throw new FinderApiException($"{response.RequestMessage?.RequestUri?.AbsolutePath} returned an empty body.", response.StatusCode);
}
