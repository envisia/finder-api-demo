using FinderDemo.Areas;
using FinderDemo.Areas.Connection;
using FinderDemo.Configuration;
using FinderDemo.Extensions;
using FinderDemo.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;

// The generic host provides configuration, logging and DI. Its content root is the output directory,
// because that is where appsettings.json is copied to.
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});
builder.Configuration.AddFinderDemoSources(args);
var settings = builder.Configuration.Get<DemoSettings>() ?? new DemoSettings();

// Whatever the configuration does not contain is asked for now.
ConnectionPrompt.AskForMissingSettings(AnsiConsole.Console, settings.Finder);

builder.Logging.AddFinderDemoSerilog(settings, AnsiConsole.Console);
builder.Services.AddFinderDemo(settings);
builder.ConfigureContainer(new DefaultServiceProviderFactory(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true }));

// The demo is interactive, so the host is only built for its services: there is no hosted service to start.
using var host = builder.Build();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

try
{
    logger.Started(settings.Finder.BaseUri, settings.Finder.ClientId, settings.ResolvedDownloadDirectory);
    await host.Services.GetRequiredService<MainMenu>().RunAsync();
    logger.Stopped();
}
catch (Exception e)
{
    // Last resort: make sure a crash ends up in the log file, then let it terminate the process as usual.
    logger.Crashed(e);
    throw;
}
