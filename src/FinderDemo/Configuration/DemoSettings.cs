using FinderDemo.Extensions;
using Microsoft.Extensions.Configuration;
using Serilog.Events;

namespace FinderDemo.Configuration;

/// <summary>
/// Settings from <c>appsettings.json</c>, <c>appsettings.Local.json</c> (git-ignored; next to the project or in the
/// directory the app is started from), environment variables
/// (prefix <c>FINDERDEMO_</c>, e.g. <c>FINDERDEMO_Finder__ClientSecret</c>) and command line
/// (e.g. <c>--Finder:ServerAddress=https://…</c>).
/// </summary>
internal sealed class DemoSettings
{
    public FinderClientOptions Finder { get; set; } = new();

    /// <summary>Gets or sets the target directory for downloads. Defaults to <c>~/Downloads/finder-demo</c>.</summary>
    public string? DownloadDirectory { get; set; }

    /// <summary>Gets or sets the directory for the log files (relative paths are resolved against the current directory).</summary>
    public string LogDirectory { get; set; } = "logs";

    public LogEventLevel LogLevel { get; set; } = LogEventLevel.Information;

    public string ResolvedDownloadDirectory => string.IsNullOrWhiteSpace(DownloadDirectory)
        ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "finder-demo")
        : DownloadDirectory.ToFullPath();

    public string ResolvedLogDirectory => (string.IsNullOrWhiteSpace(LogDirectory) ? "logs" : LogDirectory).ToFullPath();

    public static DemoSettings Load(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: true)
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.Local.json"), optional: true)
            .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Local.json"), optional: true)
            .AddEnvironmentVariables(prefix: "FINDERDEMO_")
            .AddCommandLine(args)
            .Build();

        return configuration.Get<DemoSettings>() ?? new DemoSettings();
    }
}
