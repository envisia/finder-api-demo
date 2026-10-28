using Microsoft.Extensions.Configuration;

namespace FinderDemo.Extensions;

internal static class ConfigurationBuilderExtensions
{
    /// <summary>
    /// Adds the demo's sources on top of the host defaults (<c>appsettings.json</c>, environment variables, command
    /// line): the git-ignored <c>appsettings.Local.json</c> next to the app or in the directory it is started from,
    /// and environment variables with the prefix <c>FINDERDEMO_</c> (e.g. <c>FINDERDEMO_Finder__ClientSecret</c>).
    /// The command line (e.g. <c>--Finder:ServerAddress=https://…</c>) is added again so it still wins.
    /// </summary>
    public static IConfigurationBuilder AddFinderDemoSources(this IConfigurationBuilder configuration, string[] args) =>
        configuration
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.Local.json"), optional: true)
            .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Local.json"), optional: true)
            .AddEnvironmentVariables(prefix: "FINDERDEMO_")
            .AddCommandLine(args);
}
