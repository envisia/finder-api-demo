using FinderDemo.Areas.Search;
using FinderDemo.Areas.Upload;
using FinderDemo.Configuration;
using FinderDemo.Logging;
using FinderDemo.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectre.Console;

namespace FinderDemo.Areas;

/// <summary>Checks the connection, then lets the user pick a feature until they quit.</summary>
internal sealed class MainMenu(
    IAnsiConsole console,
    IOptions<FinderClientOptions> options,
    FinderTokenProvider tokens,
    SearchArea search,
    UploadArea upload,
    ILogger<MainMenu> logger)
{
    private const string Search = "Search documents";
    private const string Upload = "Upload a document";
    private const string Quit = "Quit";

    public async Task RunAsync()
    {
        console.Write(new FigletText("envisia finder").Color(Color.Green));
        var connection = options.Value;
        console.MarkupLineInterpolated($"Server [blue]{connection.BaseUri}[/], client [blue]{connection.ClientId}[/]");
        if (!await TryConnectAsync())
        {
            return;
        }

        while (true)
        {
            console.WriteLine();
            var choice = console.Prompt(new SelectionPrompt<string>().Title("What do you want to do?").AddChoices(Search, Upload, Quit));
            if (choice == Quit)
            {
                return;
            }

            try
            {
                await (choice == Search ? search.RunAsync() : upload.RunAsync());
            }
            catch (Exception e) when (EndsFeatureOnly(e))
            {
                logger.ActionFailed(e, choice);
                console.MarkupLineInterpolated($"[red]{e.Message}[/]");
            }
        }
    }

    /// <summary>
    /// Errors the user can do something about end the feature, not the demo: API errors, network problems and
    /// timeouts (<see cref="TaskCanceledException"/> from <see cref="HttpClient"/>), file access problems and the
    /// demo's own <see cref="DemoException"/>. Anything else is a bug and terminates the demo.
    /// </summary>
    private static bool EndsFeatureOnly(Exception e) =>
        e is FinderApiException or DemoException or HttpRequestException or TaskCanceledException or IOException or UnauthorizedAccessException;

    /// <summary>Requesting a token first shows right away whether the server address and the credentials work.</summary>
    private async Task<bool> TryConnectAsync()
    {
        try
        {
            await tokens.GetAccessTokenAsync(FinderTokenProvider.ReadWriteScope);
            console.MarkupLine("[green]✓ Connected[/] (received an access token)");
            return true;
        }
        catch (Exception e) when (EndsFeatureOnly(e))
        {
            logger.ActionFailed(e, "Connecting");
            console.MarkupLineInterpolated($"[red]Could not connect: {e.Message}[/]");
            return false;
        }
    }
}
