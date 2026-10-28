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
    /// <summary>Name of the <see cref="HttpClient"/> the API requests are sent with. It adds the bearer token.</summary>
    public const string HttpClientName = "envisia-finder";

    /// <summary>
    /// Name of the <see cref="HttpClient"/> for the token endpoint. It has no <see cref="FinderAuthorizationHandler"/>,
    /// because that handler asks <see cref="FinderTokenProvider"/> for a token and would end up calling itself.
    /// </summary>
    public const string TokenClientName = "envisia-finder-token";

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
    /// Registers <see cref="FinderApiClient"/>, <see cref="FinderUploadClient"/> and <see cref="FinderTokenProvider"/>,
    /// and the named <see cref="HttpClient"/>s they send their requests with. Both log every call.
    /// </summary>
    public static IServiceCollection AddFinderServices(this IServiceCollection services, FinderClientOptions options)
    {
        services.AddSingleton(Options.Create(options));
        services.AddSingleton(TimeProvider.System);
        services.AddTransient<FinderAuthorizationHandler>();
        services.AddTransient<ApiCallLogHandler>();
        services.AddSingleton<FinderTokenProvider>();
        services.AddSingleton<FinderApiClient>();
        services.AddSingleton<FinderUploadClient>();

        // Handlers run in the order they are added: the token is attached first, then ApiCallLogHandler logs the
        // call. It replaces the default IHttpClientFactory loggers, which would log every call a second time.
        services.AddHttpClient(HttpClientName, ConfigureClient)
            .RemoveAllLoggers()
            .AddHttpMessageHandler<FinderAuthorizationHandler>()
            .AddHttpMessageHandler<ApiCallLogHandler>();
        services.AddHttpClient(TokenClientName, ConfigureClient)
            .RemoveAllLoggers()
            .AddHttpMessageHandler<ApiCallLogHandler>();
        return services;

        void ConfigureClient(HttpClient http)
        {
            http.BaseAddress = options.BaseUri;
            http.Timeout = RequestTimeout;
        }
    }
}
