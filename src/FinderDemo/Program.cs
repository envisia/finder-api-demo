using FinderDemo.Areas;
using FinderDemo.Areas.Connection;
using FinderDemo.Configuration;
using FinderDemo.Extensions;
using FinderDemo.Logging;
using FinderDemo.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Filters;
using Spectre.Console;

var settings = DemoSettings.Load(args);

// The connection comes from appsettings(.Local).json, environment variables or the command line;
// whatever is missing is asked for now.
ConnectionPrompt.AskForMissingSettings(AnsiConsole.Console, settings.Finder);

// Serilog writes the log file and prints every API call (method, path, status – never tokens) into the console.
var serilog = new LoggerConfiguration()
    .MinimumLevel.Is(settings.LogLevel)
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .WriteTo.File(
        Path.Combine(settings.ResolvedLogDirectory, "finder-demo-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .WriteTo.Logger(console => console
        .Filter.ByIncludingOnly(Matching.FromSource<ApiCallLogHandler>())
        .WriteTo.Sink(new ConsoleApiLogSink(AnsiConsole.Console)))
    .CreateLogger();

var services = new ServiceCollection()
    .AddLogging(logging => logging.AddSerilog(serilog, dispose: true))
    .AddFinderDemo(settings);
await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
var logger = provider.GetRequiredService<ILogger<Program>>();

try
{
    logger.Started(settings.Finder.BaseUri, settings.Finder.ClientId, settings.ResolvedDownloadDirectory);
    await provider.GetRequiredService<MainMenu>().RunAsync();
    logger.Stopped();
}
catch (Exception e)
{
    // Last resort: make sure a crash ends up in the log file, then let it terminate the process as usual.
    logger.Crashed(e);
    throw;
}
