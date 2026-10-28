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

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});
builder.Configuration.AddFinderDemoSources(args);
var settings = builder.Configuration.Get<DemoSettings>() ?? new DemoSettings();

ConnectionPrompt.AskForMissingSettings(AnsiConsole.Console, settings.Finder);

builder.Logging.AddFinderDemoSerilog(settings, AnsiConsole.Console);
builder.Services.AddFinderDemo(settings);
builder.ConfigureContainer(new DefaultServiceProviderFactory(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true }));

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
    logger.Crashed(e);
    throw;
}
