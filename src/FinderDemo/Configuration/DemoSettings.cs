using FinderDemo.Extensions;
using Serilog.Events;

namespace FinderDemo.Configuration;

/// <summary>
/// Settings bound from the host configuration; see <see cref="ConfigurationBuilderExtensions.AddFinderDemoSources"/>
/// for where they come from.
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
}
