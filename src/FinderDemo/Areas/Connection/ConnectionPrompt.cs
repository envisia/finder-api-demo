using FinderDemo.Configuration;
using Spectre.Console;

namespace FinderDemo.Areas.Connection;

/// <summary>
/// Asks for the connection settings that are not configured yet. The values come from the app registration in the
/// envisia finder: <c>Systemeinstellungen › App-Registrierungen › Hinzufügen</c>, type "envisia finder API".
/// </summary>
internal static class ConnectionPrompt
{
    public static void AskForMissingSettings(IAnsiConsole console, FinderClientOptions options)
    {
        if (options.IsComplete)
        {
            return;
        }

        console.MarkupLine("[yellow]The connection to the envisia finder is not configured completely.[/]");
        console.MarkupLine("Enter the missing values of the app registration, or put them into [blue]appsettings.Local.json[/].");

        if (!FinderClientOptions.IsValidServerAddress(options.ServerAddress))
        {
            options.ServerAddress = console.Prompt(
                new TextPrompt<string>("Server address (e.g. https://customer.envisia-finder.de):")
                    .Validate(value => FinderClientOptions.IsValidServerAddress(value)
                        ? ValidationResult.Success()
                        : ValidationResult.Error("Please enter an http(s) address.")));
        }

        if (string.IsNullOrWhiteSpace(options.ClientId))
        {
            options.ClientId = console.Prompt(new TextPrompt<string>("Client ID:"));
        }

        if (string.IsNullOrWhiteSpace(options.ClientSecret))
        {
            options.ClientSecret = console.Prompt(new TextPrompt<string>("Client secret:").Secret());
        }

        // Optional: act as this user via token exchange (needs the scope finder.impersonation). A configured user is kept.
        if (string.IsNullOrWhiteSpace(options.ImpersonateUpn))
        {
            var upn = console.Prompt(new TextPrompt<string>("Act as user (UPN, optional):").AllowEmpty());
            options.ImpersonateUpn = string.IsNullOrWhiteSpace(upn) ? null : upn.Trim();
        }
    }
}
