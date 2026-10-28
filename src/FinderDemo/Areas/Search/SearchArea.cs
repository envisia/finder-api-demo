using System.Text.Json;
using FinderDemo.Logging;
using FinderDemo.Models.Additionals;
using FinderDemo.Models.Search;
using FinderDemo.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace FinderDemo.Areas.Search;

/// <summary>
/// Searches document folders with <c>POST /api/v1/Search/Document</c>: by name, EfinGuid, full text, or folders where
/// additionals are <b>not set</b> (<c>empty_additionals</c>), then pages through the hits and opens a folder.
/// </summary>
internal sealed class SearchArea(IAnsiConsole console, FinderApiClient api, FolderArea folders, ILogger<SearchArea> logger)
{
    private const int PageSize = 10;

    private const string OpenFolder = "Open a folder";
    private const string NextPage = "Next page";
    private const string PreviousPage = "Previous page";
    private const string Back = "Back to the menu";

    public async Task RunAsync()
    {
        console.Write(new Rule("[green]Search[/]").LeftJustified());
        var request = await AskForSearchAsync();
        DocumentSearchResult? result = null;

        while (true)
        {
            if (result is null)
            {
                ShowRequest(request);
                result = await api.SearchDocumentsAsync(request);
                logger.SearchCompleted(result.TotalHits);
            }

            var hits = result.Hits ?? [];
            ShowResults(result, request.FromHit);

            var actions = new List<string>();
            if (hits.Count > 0)
            {
                actions.Add(OpenFolder);
            }

            if (request.FromHit + hits.Count < result.TotalHits)
            {
                actions.Add(NextPage);
            }

            if (request.FromHit > 0)
            {
                actions.Add(PreviousPage);
            }

            actions.Add(Back);
            switch (console.Prompt(new SelectionPrompt<string>().Title("What next?").AddChoices(actions)))
            {
                case OpenFolder:
                    var hit = console.Prompt(
                        new SelectionPrompt<DocumentSearchHit>()
                            .Title("Which folder?")
                            .UseConverter(h => Markup.Escape($"{h.Name}.{h.Extension}  ({h.Id})"))
                            .AddChoices(hits));
                    await folders.ShowAsync(hit);
                    break;
                case NextPage:
                    request = request with { FromHit = request.FromHit + PageSize };
                    result = null;
                    break;
                case PreviousPage:
                    request = request with { FromHit = Math.Max(0, request.FromHit - PageSize) };
                    result = null;
                    break;
                default:
                    return;
            }
        }
    }

    private static string Describe(SearchMode mode) => mode switch
    {
        SearchMode.Name => "Folder name",
        SearchMode.EfinGuid => "EfinGuid of the folder",
        SearchMode.FullText => "Full text",
        _ => "Folders where an additional is [red]not set[/]",
    };

    private async Task<DocumentSearchRequest> AskForSearchAsync()
    {
        var mode = console.Prompt(
            new SelectionPrompt<SearchMode>()
                .Title("Search by")
                .UseConverter(Describe)
                .AddChoices(Enum.GetValues<SearchMode>()));

        return mode switch
        {
            SearchMode.Name => new DocumentSearchRequest { Name = console.Ask<string>("Folder name:"), HitsSize = PageSize },
            SearchMode.EfinGuid => new DocumentSearchRequest { Id = console.Ask<string>("EfinGuid (efin_…):"), HitsSize = PageSize },
            SearchMode.FullText => new DocumentSearchRequest { Query = console.Ask<string>("Text:"), HitsSize = PageSize },
            _ => new DocumentSearchRequest { EmptyAdditionals = await AskForUnsetAdditionalsAsync(), HitsSize = PageSize },
        };
    }

    /// <summary>
    /// <c>empty_additionals</c> takes the IDs of additionals a folder must not have. Additionals are configured per
    /// location, so they are loaded for all process steps and archive categories the user can see.
    /// </summary>
    private async Task<List<int>> AskForUnsetAdditionalsAsync()
    {
        var processSteps = await api.GetProcessStepsAsync();
        var archiveCategories = await api.GetArchiveCategoriesAsync();
        var locationIds = processSteps.Concat(archiveCategories).Select(c => c.Id).ToList();
        var additionals = locationIds.Count == 0 ? [] : await api.GetCategoryAdditionalsAsync(locationIds);
        if (additionals.Count == 0)
        {
            throw new DemoException("No additionals are configured in the locations you can see.");
        }

        var chosen = console.Prompt(
            new MultiSelectionPrompt<CategoryAdditional>()
                .Title("Find folders where these additionals are [red]not set[/]:")
                .InstructionsText("[grey](Space to select, Enter to confirm)[/]")
                .UseConverter(a => Markup.Escape($"{a.Title} (#{a.Id}, {a.ValueType})"))
                .AddChoices(additionals.DistinctBy(a => a.Id).OrderBy(a => a.Title, StringComparer.CurrentCultureIgnoreCase)));
        console.MarkupLineInterpolated($"Not set: {string.Join(", ", chosen.Select(a => a.Title))}");
        return chosen.Select(a => a.Id).ToList();
    }

    private void ShowRequest(DocumentSearchRequest request) =>
        console.Write(
            new Panel(new Text(JsonSerializer.Serialize(request, FinderJson.Indented), new Style(Color.Grey)))
                .Header("POST /api/v1/Search/Document")
                .BorderColor(Color.Grey)
                .Expand());

    private void ShowResults(DocumentSearchResult result, int fromHit)
    {
        var hits = result.Hits ?? [];
        var table = new Table()
            .Caption($"{result.TotalHits:N0} folder(s) found, showing {(hits.Count == 0 ? 0 : fromHit + 1)}–{fromHit + hits.Count}")
            .AddColumns("Name", "Ext", "Archived", "Locations", "Folder ID (EfinGuid)");
        foreach (var hit in hits)
        {
            table.AddRow(
                Markup.Escape(hit.Name ?? string.Empty),
                Markup.Escape(hit.Extension ?? string.Empty),
                hit.ArchiveDate?.ToString("dd.MM.yyyy") ?? string.Empty,
                Markup.Escape(string.Join(", ", hit.Categories ?? [])),
                Markup.Escape(hit.Id));
        }

        console.Write(table);
    }
}
