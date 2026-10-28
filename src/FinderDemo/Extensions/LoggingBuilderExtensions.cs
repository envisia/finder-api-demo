using FinderDemo.Configuration;
using FinderDemo.Logging;
using FinderDemo.Services;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Filters;
using Spectre.Console;

namespace FinderDemo.Extensions;

internal static class LoggingBuilderExtensions
{
    /// <summary>
    /// Replaces the default logging providers with Serilog. It writes the log file and prints every API call
    /// (method, path, status – never headers or bodies, so never tokens) into the console.
    /// </summary>
    public static ILoggingBuilder AddFinderDemoSerilog(this ILoggingBuilder logging, DemoSettings settings, IAnsiConsole console)
    {
        var serilog = new LoggerConfiguration()
            .MinimumLevel.Is(settings.LogLevel)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .WriteTo.File(
                Path.Combine(settings.ResolvedLogDirectory, "finder-demo-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .WriteTo.Logger(apiCalls => apiCalls
                .Filter.ByIncludingOnly(Matching.FromSource<ApiCallLogHandler>())
                .WriteTo.Sink(new ConsoleApiLogSink(console)))
            .CreateLogger();

        // The host's default console logger would write into the Spectre.Console UI, so Serilog is the only provider.
        return logging.ClearProviders().AddSerilog(serilog, dispose: true);
    }
}
