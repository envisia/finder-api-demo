using FinderDemo.Configuration;
using FinderDemo.Models.Documents;
using FinderDemo.Models.Search;
using FinderDemo.Services;
using Spectre.Console;

namespace FinderDemo.Areas.Search;

/// <summary>
/// Shows a document folder (<c>GET /api/v2/Document/{efinGuid}</c>) and downloads its documents
/// (<c>GET /api/v2/media/{file}</c>) or shows their recognized text (<c>GET /api/v2/Document/File/{efinGuid}/content</c>).
/// </summary>
internal sealed class FolderArea(IAnsiConsole console, FinderApiClient api, DemoSettings settings)
{
    private const string Download = "Download a document";
    private const string ShowText = "Show the text of a document (page 1)";
    private const string Back = "Back";

    public async Task ShowAsync(DocumentSearchHit hit)
    {
        var folder = await api.GetFolderAsync(hit.Id);
        console.Write(new Rule($"[green]{Markup.Escape(folder.Title ?? hit.Name ?? hit.Id)}[/]").LeftJustified());
        console.Write(Details(hit, folder));

        var documents = folder.Documents ?? [];
        var table = new Table().AddColumns("Document", "Pages", "Date", "Document ID (EfinGuid)");
        foreach (var document in documents)
        {
            var title = (document.Pinned ? "(pinned) " : string.Empty) + document.Title;
            table.AddRow(Markup.Escape(title), $"{document.PageCount}", $"{document.Date:dd.MM.yyyy HH:mm}", Markup.Escape(document.EfinGuid));
        }

        console.Write(table);

        while (documents.Count > 0)
        {
            var action = console.Prompt(new SelectionPrompt<string>().Title("What next?").AddChoices(Download, ShowText, Back));
            if (action == Back)
            {
                return;
            }

            var chosen = documents.Count == 1
                ? documents[0]
                : console.Prompt(
                    new SelectionPrompt<FolderDocument>()
                        .Title("Which document?")
                        .UseConverter(d => Markup.Escape($"{d.Title} ({d.EfinGuid})"))
                        .AddChoices(documents));

            if (action == Download)
            {
                // Downloads need an access token with the scope finder.api.media; FinderApiClient requests it separately.
                var file = await api.DownloadDocumentAsync(chosen.EfinGuid, settings.ResolvedDownloadDirectory);
                console.MarkupLineInterpolated($"[green]Saved[/] {file.Path} ({file.Length:N0} bytes)");
            }
            else
            {
                var text = await api.GetDocumentTextAsync(chosen.EfinGuid, page: 1);
                console.Write(new Panel(new Text(string.IsNullOrWhiteSpace(text) ? "(no recognized text)" : text)).Header(Markup.Escape(chosen.Title ?? chosen.EfinGuid)));
            }
        }
    }

    private static Grid Details(DocumentSearchHit hit, Folder folder)
    {
        var grid = new Grid().AddColumn(new GridColumn().NoWrap()).AddColumn();
        void Row(string label, string? value) => grid.AddRow($"[grey]{Markup.Escape(label)}[/]", Markup.Escape(value ?? "–"));

        Row("Folder ID", hit.Id);
        Row("Newest document", hit.CurrentFileId);
        Row("Archived", hit.ArchiveDate?.ToString("dd.MM.yyyy HH:mm"));
        Row("Locations", string.Join(", ", hit.Categories ?? []));
        Row("File numbers", hit.FileNumbers is { Count: > 0 } numbers ? string.Join(", ", numbers) : null);
        Row("Business partner", folder.Partner?.Name is { } partner ? $"{partner} ({folder.Partner.Number})" : null);
        Row("Web URL", hit.WebUrl);

        // Only filled when the search had a full text query.
        if (!string.IsNullOrWhiteSpace(hit.Content))
        {
            Row("Full text match", hit.Content);
        }

        // multiple_values_additionals has all values; additionals only the first value of each.
        var additionals = hit.MultipleValuesAdditionals?.Select(a => (a.Key, Value: string.Join("; ", a.Value)))
                          ?? hit.Additionals?.Select(a => (a.Key, a.Value))
                          ?? [];
        foreach (var (title, value) in additionals)
        {
            Row(title, value);
        }

        return grid;
    }
}
