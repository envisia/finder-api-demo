using System.Net;
using System.Text.Json;
using FinderDemo.Extensions;
using FinderDemo.Models.Errors;

namespace FinderDemo.Services;

/// <summary>An API call returned an unexpected HTTP status code.</summary>
public class FinderApiException(string message, HttpStatusCode? statusCode = null) : Exception(message)
{
    private const int MaxBodyLength = 500;

    public HttpStatusCode? StatusCode { get; } = statusCode;

    internal static async Task<FinderApiException> FromResponseAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var request = response.RequestMessage;
        var message = $"{request?.Method} {request?.RequestUri?.AbsolutePath} failed with {(int)response.StatusCode} {response.ReasonPhrase}";

        var hint = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "The access token was rejected.",
            HttpStatusCode.Forbidden => "The app/user lacks the permission (or OAuth scope) for this endpoint.",
            HttpStatusCode.NotFound => "The requested resource does not exist.",
            _ => null,
        };
        if (hint is not null)
        {
            message += $". {hint}";
        }

        // Problem details are read as such; any other body (plain text, HTML of a proxy, …) is shown as is.
        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var detail = response.HasMediaType(MediaTypes.ProblemJson)
            ? JsonSerializer.Deserialize<ProblemDetailsResponse>(body, FinderJson.Options)?.Describe()
            : Truncate(body);
        if (!string.IsNullOrWhiteSpace(detail))
        {
            message += Environment.NewLine + detail;
        }

        return new FinderApiException(message, response.StatusCode);
    }

    private static string Truncate(string body) => body.Length > MaxBodyLength ? body[..MaxBodyLength] + "…" : body;
}
