using FinderDemo.Areas;
using FinderDemo.Areas.Search;
using FinderDemo.Areas.Upload;
using FinderDemo.Configuration;
using FinderDemo.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Spectre.Console;

namespace FinderDemo.Extensions;

internal static class ServiceCollectionExtensions
{
    /// <summary>Name of the <see cref="HttpClient"/> all finder requests (including the token endpoint) are sent with.</summary>
    public const string HttpClientName = "envisia-finder";

    /// <summary>Generous timeout: a single upload fragment can be up to 20 MiB on a slow line.</summary>
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromMinutes(5);

    /// <summary>Registers the finder services and the console areas.</summary>
    public static IServiceCollection AddFinderDemo(this IServiceCollection services, DemoSettings settings)
    {
        services.AddSingleton(settings);
        services.AddSingleton(AnsiConsole.Console);
        services.AddFinderServices(settings.Finder);

        services.AddSingleton<MainMenu>();
        services.AddSingleton<SearchArea>();
        services.AddSingleton<FolderArea>();
        services.AddSingleton<UploadArea>();
        services.AddSingleton<AdditionalPrompts>();
        return services;
    }

    /// <summary>
    /// Registers <see cref="FinderApiClient"/>, <see cref="FinderUploadClient"/> and <see cref="FinderTokenProvider"/>.
    /// All of them send their requests through one named <see cref="HttpClient"/> that logs every call.
    /// </summary>
    public static IServiceCollection AddFinderServices(this IServiceCollection services, FinderClientOptions options)
    {
        services.AddSingleton(Options.Create(options));
        services.AddSingleton(TimeProvider.System);
        services.AddTransient<ApiCallLogHandler>();
        services.AddSingleton<FinderTokenProvider>();
        services.AddSingleton<FinderApiClient>();
        services.AddSingleton<FinderUploadClient>();

        // ApiCallLogHandler logs each call; the default IHttpClientFactory loggers would log it a second time.
        services.AddHttpClient(HttpClientName, http =>
            {
                http.BaseAddress = options.BaseUri;
                http.Timeout = RequestTimeout;
            })
            .RemoveAllLoggers()
            .AddHttpMessageHandler<ApiCallLogHandler>();
        return services;
    }
}
