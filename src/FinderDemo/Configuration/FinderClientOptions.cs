namespace FinderDemo.Configuration;

/// <summary>
/// Connection settings. The values correspond to the configuration file that can be downloaded in the
/// envisia finder under <c>Systemeinstellungen › App-Registrierungen</c> (application type "envisia finder API").
/// </summary>
public sealed class FinderClientOptions
{
    /// <summary>Gets or sets the base address of the envisia finder, e.g. <c>https://customer.envisia-finder.de</c>.</summary>
    public string ServerAddress { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional user principal name (usually the e-mail address). When set, every API call is executed
    /// as this user via OAuth token exchange (RFC 8693). Requires the scope <c>finder.impersonation</c> for the app.
    /// </summary>
    public string? ImpersonateUpn { get; set; }

    public bool IsComplete =>
        IsValidServerAddress(ServerAddress) &&
        !string.IsNullOrWhiteSpace(ClientId) &&
        !string.IsNullOrWhiteSpace(ClientSecret);

    /// <summary>Gets the server address normalized with a trailing slash so relative request URIs resolve correctly.</summary>
    public Uri BaseUri => new(ServerAddress.TrimEnd('/') + "/", UriKind.Absolute);

    /// <summary>
    /// An absolute http(s) address. <see cref="Uri.TryCreate(string, UriKind, out Uri)"/> alone is not enough:
    /// it reads <c>localhost:5099</c> as an absolute URI with the scheme <c>localhost</c>.
    /// </summary>
    public static bool IsValidServerAddress(string? address) =>
        Uri.TryCreate(address, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
}
