using System.Diagnostics;
using FinderDemo.Logging;
using Microsoft.Extensions.Logging;

namespace FinderDemo.Services;

/// <summary>
/// Logs every request with method, path, status code and duration. Headers and bodies are never logged, so tokens
/// and client secrets cannot end up in a log. Registered by <c>AddFinderServices</c>, which removes the default
/// <see cref="IHttpClientFactory"/> loggers so calls are not logged twice.
/// </summary>
public sealed class ApiCallLogHandler(ILogger<ApiCallLogHandler> logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var path = request.RequestUri?.PathAndQuery ?? string.Empty;
        if (request.Content?.Headers.ContentRange is { } range)
        {
            path += $" [{range}]";
        }

        try
        {
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var level = (int)response.StatusCode >= 400 ? LogLevel.Warning : LogLevel.Information;
            logger.HttpCallCompleted(level, request.Method.Method, path, (int)response.StatusCode, stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception e)
        {
            // Only records the failure; the exception is rethrown unchanged.
            logger.HttpCallFailed(e, request.Method.Method, path, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
