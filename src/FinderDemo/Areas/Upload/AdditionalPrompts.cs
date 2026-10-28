using System.Globalization;
using FinderDemo.Models.Additionals;
using FinderDemo.Models.Uploads;
using FinderDemo.Services;
using Spectre.Console;

namespace FinderDemo.Areas.Upload;

/// <summary>
/// Asks for the values of additionals ("Zusatzinformationen"), with a prompt matching each <c>value_type</c>:
/// text, number, date, a choice from <c>GET /api/v2/additional/select/{id}</c> or a checkbox. Text additionals with
/// <c>suggestion</c> offer existing values from <c>GET /api/v2/additional/suggestion/{id}</c>.
/// </summary>
internal sealed class AdditionalPrompts(IAnsiConsole console, FinderApiClient api)
{
    private const string OtherValue = "(enter a different value)";

    /// <summary>The select endpoint returns 10 options per page; with more pages the user has to filter.</summary>
    private const int MaxSelectPages = 20;

    /// <summary>Asks for all mandatory additionals and for the optional ones the user picks.</summary>
    public async Task<List<UploadSessionAdditional>> AskAsync(IReadOnlyList<CategoryAdditional> definitions)
    {
        var optional = definitions.Where(d => !d.IsMandatory).ToList();
        var chosen = optional.Count == 0
            ? []
            : console.Prompt(
                new MultiSelectionPrompt<CategoryAdditional>()
                    .Title("Which optional additionals do you want to fill in?")
                    .NotRequired()
                    .InstructionsText("[grey](Space to select, Enter to confirm)[/]")
                    .UseConverter(Describe)
                    .AddChoices(optional));

        if (chosen.Count > 0)
        {
            console.MarkupLineInterpolated($"Optional additionals: {string.Join(", ", chosen.Select(d => d.Title))}");
        }

        var values = new List<UploadSessionAdditional>();
        foreach (var definition in definitions.Where(d => d.IsMandatory).Concat(chosen))
        {
            values.Add(await AskAsync(definition));
        }

        return values;
    }

    public async Task<UploadSessionAdditional> AskAsync(CategoryAdditional definition)
    {
        List<string> values = definition.ValueType switch
        {
            AdditionalValueTypes.Select => await AskSelectAsync(definition),
            AdditionalValueTypes.Checkbox => [console.Confirm($"{Markup.Escape(definition.Title)}?") ? "true" : "false"],
            AdditionalValueTypes.String when definition.Suggestion => await AskWithSuggestionsAsync(definition),
            _ => AskText(definition),
        };

        // Several values go into "values", a single one into "value".
        return definition.MultipleValues
            ? new UploadSessionAdditional { Id = definition.Id, Values = values }
            : new UploadSessionAdditional { Id = definition.Id, Value = values[0] };
    }

    public static string Describe(CategoryAdditional d)
    {
        var flags = new List<string> { d.ValueType };
        if (d.IsMandatory)
        {
            flags.Add("mandatory");
        }

        if (d.MultipleValues)
        {
            flags.Add("multiple values");
        }

        return Markup.Escape($"{d.Title} (#{d.Id}, {string.Join(", ", flags)})");
    }

    /// <summary>Text, number or date. Dates use dd.MM.yyyy, numbers a dot as decimal separator.</summary>
    private List<string> AskText(CategoryAdditional definition)
    {
        var hint = definition.ValueType switch
        {
            AdditionalValueTypes.Date => AdditionalValueTypes.DateFormat,
            AdditionalValueTypes.Decimal => "number, e.g. 42.69",
            _ => "text",
        };
        if (definition.MultipleValues)
        {
            hint += ", separate several values with ;";
        }

        // With a default value Spectre appends " (default):" itself.
        var label = $"{Markup.Escape(definition.Title)} [grey]({Markup.Escape(hint)})[/]";
        var prompt = new TextPrompt<string>(definition.ValueType == AdditionalValueTypes.Date ? label : label + ":")
            .Validate(input => Validate(definition, Split(definition, input)) is { } error
                ? ValidationResult.Error(Markup.Escape(error))
                : ValidationResult.Success());
        if (definition.ValueType == AdditionalValueTypes.Date)
        {
            prompt.DefaultValue(DateTime.Today.ToString(AdditionalValueTypes.DateFormat, CultureInfo.InvariantCulture));
        }

        return Split(definition, console.Prompt(prompt));
    }

    private async Task<List<string>> AskWithSuggestionsAsync(CategoryAdditional definition)
    {
        var term = console.Prompt(
            new TextPrompt<string>($"{Markup.Escape(definition.Title)} [grey](type part of a value to get suggestions)[/]:").AllowEmpty());
        var suggestions = await api.GetSuggestionsAsync(definition.Id, term);
        if (suggestions.Count == 0)
        {
            return AskText(definition);
        }

        var choice = console.Prompt(
            new SelectionPrompt<string>()
                .Title($"Suggestions for {Markup.Escape(definition.Title)}:")
                .UseConverter(Markup.Escape)
                .AddChoices(suggestions.Append(OtherValue)));
        if (choice == OtherValue)
        {
            return AskText(definition);
        }

        Echo(definition, [choice]);
        return [choice];
    }

    /// <summary>
    /// Only predefined options are accepted. The endpoint pages them (10 per page, starting at 1) and can filter them;
    /// if there are too many to list, the user narrows them down with a filter.
    /// </summary>
    private async Task<List<string>> AskSelectAsync(CategoryAdditional definition)
    {
        string? filter = null;
        while (true)
        {
            var (options, complete) = await LoadSelectOptionsAsync(definition, filter);
            if (options.Count == 0 && filter is null)
            {
                throw new DemoException($"The select additional \"{definition.Title}\" has no options configured.");
            }

            if (options.Count == 0 || !complete)
            {
                console.MarkupLine(options.Count == 0
                    ? "[yellow]No option matches the filter.[/]"
                    : $"[yellow]More than {options.Count} options.[/]");
                filter = console.Prompt(new TextPrompt<string>($"Filter the options of {Markup.Escape(definition.Title)}:"));
                continue;
            }

            List<string> values = definition.MultipleValues
                ? console.Prompt(
                    new MultiSelectionPrompt<string>()
                        .Title($"{Markup.Escape(definition.Title)}:")
                        .InstructionsText("[grey](Space to select, Enter to confirm)[/]")
                        .UseConverter(Markup.Escape)
                        .AddChoices(options))
                : [console.Prompt(new SelectionPrompt<string>().Title($"{Markup.Escape(definition.Title)}:").EnableSearch().UseConverter(Markup.Escape).AddChoices(options))];
            Echo(definition, values);
            return values;
        }
    }

    /// <summary>Loads up to <see cref="MaxSelectPages"/> pages; <c>Complete</c> tells whether that were all options.</summary>
    private async Task<(List<string> Options, bool Complete)> LoadSelectOptionsAsync(CategoryAdditional definition, string? filter)
    {
        var options = new List<string>();
        for (var page = 1; page <= MaxSelectPages; page++)
        {
            var selection = await api.GetSelectOptionsAsync(definition.Id, filter, page);
            options.AddRange(selection.EntryValues ?? []);
            if (!selection.HasNext)
            {
                return (options, true);
            }
        }

        return (options, false);
    }

    /// <summary>Selection prompts disappear after the choice; this keeps the chosen value visible.</summary>
    private void Echo(CategoryAdditional definition, List<string> values) =>
        console.MarkupLineInterpolated($"{definition.Title}: {string.Join("; ", values)}");

    private static List<string> Split(CategoryAdditional definition, string input) =>
        (definition.MultipleValues ? input.Split(';') : [input])
            .Select(v => v.Trim())
            .Where(v => v.Length > 0)
            .Select(v => definition.ValueType == AdditionalValueTypes.Decimal ? v.Replace(',', '.') : v)
            .ToList();

    /// <summary>
    /// Client side check of the documented formats; <c>POST /api/v2/Additional/Validate</c> checks again, including
    /// the <c>regex</c> configured for the additional.
    /// </summary>
    private static string? Validate(CategoryAdditional d, List<string> values)
    {
        if (values.Count == 0)
        {
            return "Please enter a value.";
        }

        foreach (var value in values)
        {
            if (d.ValueType == AdditionalValueTypes.Date &&
                !DateTime.TryParseExact(value, AdditionalValueTypes.DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                return $"\"{value}\" is not a date in the format {AdditionalValueTypes.DateFormat}.";
            }

            if (d.ValueType == AdditionalValueTypes.Decimal)
            {
                if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
                {
                    return $"\"{value}\" is not a number.";
                }

                if ((d.MinValue is { } min && (double)number < min) || (d.MaxValue is { } max && (double)number > max))
                {
                    return $"{value} is outside the range {d.MinValue}–{d.MaxValue}.";
                }
            }

            if (d.ValueType == AdditionalValueTypes.String &&
                ((d.MinValue is { } minLength && value.Length < minLength) || (d.MaxValue is { } maxLength && value.Length > maxLength)))
            {
                return $"The text must have {d.MinValue}–{d.MaxValue} characters.";
            }
        }

        return null;
    }
}
