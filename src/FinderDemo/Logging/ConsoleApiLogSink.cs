using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;
using Spectre.Console;

namespace FinderDemo.Logging;

/// <summary>
/// Serilog sink that prints log events dimmed into the console, so you see every API call a step makes.
/// <c>Program</c> routes only the messages of <c>ApiCallLogHandler</c> here.
/// </summary>
internal sealed class ConsoleApiLogSink(IAnsiConsole console) : ILogEventSink
{
    private readonly MessageTemplateTextFormatter _formatter = new("{Message:lj}");

    public void Emit(LogEvent logEvent)
    {
        using var writer = new StringWriter();
        _formatter.Format(logEvent, writer);
        var color = logEvent.Level >= LogEventLevel.Warning ? "yellow" : "grey";
        console.MarkupLine($"[{color}]  ⇄ {Markup.Escape(writer.ToString())}[/]");
    }
}
