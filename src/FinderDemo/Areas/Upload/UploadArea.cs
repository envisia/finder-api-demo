using System.Diagnostics;
using FinderDemo.Areas.Search;
using FinderDemo.Extensions;
using FinderDemo.Logging;
using FinderDemo.Models.Additionals;
using FinderDemo.Models.Locations;
using FinderDemo.Models.Search;
using FinderDemo.Models.Uploads;
using FinderDemo.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace FinderDemo.Areas.Upload;

/// <summary>
/// Uploads a document as a new folder, step by step – the order an integration has to follow:
/// target location → additionals of that location → server-side validation → upload session → processing.
/// </summary>
internal sealed class UploadArea(
    IAnsiConsole console,
    FinderApiClient api,
    FinderUploadClient uploads,
    AdditionalPrompts additionalPrompts,
    FolderArea folders,
    ILogger<UploadArea> logger)
{
    private static readonly TimeSpan ProcessingTimeout = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

    public async Task RunAsync()
    {
        console.Write(new Rule("[green]Upload[/]").LeftJustified());

        // Step 1: the file. The API does not accept empty files.
        var path = console.Prompt(
            new TextPrompt<string>("File to upload:")
                .Validate(p => new FileInfo(p.ToFullPath()) switch
                {
                    { Exists: false } => ValidationResult.Error("File not found."),
                    { Length: 0 } => ValidationResult.Error("The file is empty."),
                    _ => ValidationResult.Success(),
                })).ToFullPath();

        // Step 2: where the new folder goes – exactly one process step, or one or more archive categories.
        // POST /api/v1/Category/Process or POST /api/v1/Category/Archive
        var kind = console.Prompt(
            new SelectionPrompt<LocationKind>()
                .Title("Add the document to")
                .UseConverter(k => k == LocationKind.ProcessStep ? "a process step" : "archive categories")
                .AddChoices(LocationKind.ProcessStep, LocationKind.ArchiveCategory));
        var locations = await api.GetLocationsAsync(kind);
        if (locations.Count == 0)
        {
            console.MarkupLine("[yellow]You cannot add documents to any location of this kind.[/]");
            return;
        }

        var categories = AskForLocations(kind, locations);
        var categoryIds = categories.Select(c => c.Id).ToList();
        console.MarkupLineInterpolated($"Target: {string.Join(", ", categories.Select(c => $"{c.DisplayName} (#{c.Id})"))}");

        // Step 3: the additionals available in these locations.
        // POST /api/v1/Category/Additionals
        var definitions = (await api.GetCategoryAdditionalsAsync(categoryIds)).DistinctBy(d => d.Id).ToList();
        console.MarkupLine($"{definitions.Count} additional(s) available, {definitions.Count(d => d.IsMandatory)} mandatory.");
        var additionals = await additionalPrompts.AskAsync(definitions);

        // Step 4: let the server check the values for these locations.
        // POST /api/v2/Additional/Validate – 204 if valid, otherwise 400 with the IDs of the invalid additionals.
        additionals = await ValidateAsync(categoryIds, definitions, additionals);

        // Step 5: optional metadata the user / location allows, and the checksum.
        // GET /api/v2/Upload/Rights
        var rights = await api.GetUploadRightsAsync();
        var request = new CreateUploadSessionRequest
        {
            Name = Path.GetFileName(path),
            Categories = categoryIds,
            Additionals = additionals,
            AdditionalWords = categories.Any(c => c.AdditionalWords) ? AskOptional("Keywords") : null,
            FileNumbers = rights.AddFileNumber ? AskOptional("File numbers (separate with ;)")?.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries) : null,
        };
        var checksum = console.Prompt(
            new SelectionPrompt<UploadChecksum>()
                .Title("Checksum the server verifies after the last fragment")
                .UseConverter(c => c == UploadChecksum.Md5 ? "MD5 (md5Hash)" : "CRC-32C (crc32cHash)")
                .AddChoices(UploadChecksum.Md5, UploadChecksum.Crc32C));
        console.MarkupLineInterpolated($"Checksum: {checksum}");

        // Step 6: upload session – POST /api/v2/CreateUploadSession, then one PUT /api/v2/FileUploadSession/{key}
        // per fragment (see FinderUploadClient).
        var completed = await UploadWithProgressAsync(path, request, checksum);
        console.MarkupLineInterpolated($"[green]✓ Uploaded[/], temporaryName [blue]{completed.TemporaryName}[/]");

        // Step 7: the finder processes the document in the background.
        // POST /api/v1/Search/Uploaded – 204 until it has been processed, then the new folder.
        var processed = await console.Status().StartAsync(
            "Waiting until the finder has processed the document…",
            _ => WaitForProcessingAsync(completed.TemporaryName));
        if (processed is null)
        {
            logger.UploadNotProcessed(completed.TemporaryName, ProcessingTimeout);
            console.MarkupLine("[yellow]Not processed yet – it will show up in the search once the finder is done.[/]");
            return;
        }

        logger.UploadProcessed(processed.Id);
        console.MarkupLineInterpolated($"[green]✓ Processed[/]: folder [blue]{processed.Id}[/] {processed.WebUrl}");
        if (console.Confirm("Open the new folder?"))
        {
            var search = await api.SearchDocumentsAsync(new DocumentSearchRequest { Id = processed.Id });
            if (search.Hits is [var hit, ..])
            {
                await folders.ShowAsync(hit);
            }
        }
    }

    private List<Category> AskForLocations(LocationKind kind, List<Category> locations)
    {
        static string Display(Category c) => Markup.Escape($"{c.DisplayName} (#{c.Id})");

        return kind == LocationKind.ProcessStep
            ? [console.Prompt(new SelectionPrompt<Category>().Title("Process step:").UseConverter(Display).AddChoices(locations))]
            : console.Prompt(
                new MultiSelectionPrompt<Category>()
                    .Title("Archive categories:")
                    .InstructionsText("[grey](Space to select, Enter to confirm)[/]")
                    .UseConverter(Display)
                    .AddChoices(locations));
    }

    /// <summary>Validates on the server and asks again for every additional it rejects.</summary>
    private async Task<List<UploadSessionAdditional>> ValidateAsync(
        List<int> categoryIds,
        List<CategoryAdditional> definitions,
        List<UploadSessionAdditional> additionals)
    {
        while (true)
        {
            var rejected = await api.ValidateAdditionalsAsync(new AdditionalValidationRequest { Categories = categoryIds, Additionals = additionals });
            if (rejected.Count == 0)
            {
                console.MarkupLine("[green]✓ The server accepted the additionals.[/]");
                return additionals;
            }

            logger.AdditionalsRejected(rejected);

            // Asking again only helps for additionals the user can enter; anything else would be rejected forever.
            var unknown = rejected.Where(id => definitions.All(d => d.Id != id)).ToList();
            if (unknown.Count > 0)
            {
                throw new DemoException($"The server rejected additionals that are not available for these locations: {string.Join(", ", unknown)}.");
            }

            foreach (var definition in definitions.Where(d => rejected.Contains(d.Id)))
            {
                console.MarkupLine($"[red]The server rejected[/] {AdditionalPrompts.Describe(definition)}");
                additionals.RemoveAll(a => a.Id == definition.Id);
                additionals.Add(await additionalPrompts.AskAsync(definition));
            }
        }
    }

    private string? AskOptional(string label)
    {
        var value = console.Prompt(new TextPrompt<string>($"{label} [grey](optional)[/]:").AllowEmpty());
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private Task<UploadSessionCompleted> UploadWithProgressAsync(string path, CreateUploadSessionRequest request, UploadChecksum checksum) =>
        console.Progress().StartAsync(async context =>
        {
            var task = context.AddTask($"Uploading {Markup.Escape(request.Name)}", maxValue: new FileInfo(path).Length);
            var progress = new SynchronousProgress<UploadProgress>(p =>
            {
                task.Description = p.Stage switch
                {
                    UploadStage.Hashing => $"Computing the {checksum} checksum",
                    UploadStage.CreatingSession => "Creating the upload session",
                    UploadStage.Retrying => "Fragment failed, resuming",
                    _ => $"Uploading {Markup.Escape(request.Name)}",
                };
                task.Value = p.BytesSent;
            });

            var completed = await uploads.UploadFileAsync(path, request, progress, checksum);
            task.Value = task.MaxValue;
            return completed;
        });

    private async Task<UploadedSearchHit?> WaitForProcessingAsync(string temporaryName)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < ProcessingTimeout)
        {
            if (await api.FindUploadedDocumentAsync(temporaryName) is { } hit)
            {
                return hit;
            }

            await Task.Delay(PollInterval);
        }

        return null;
    }
}
