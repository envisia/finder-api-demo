using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinderDemo.Configuration;
using FinderDemo.Extensions;
using FinderDemo.Logging;
using FinderDemo.Models.Additionals;
using FinderDemo.Models.Documents;
using FinderDemo.Models.Locations;
using FinderDemo.Models.Search;
using FinderDemo.Models.Uploads;
using Microsoft.Extensions.Logging;

namespace FinderDemo.Services;

/// <summary>
/// Thin client for the envisia finder REST API (search, locations, additionals, folders, downloads).
/// Uploads live in <see cref="FinderUploadClient"/>. Both are registered by <c>AddFinderServices</c>.
/// </summary>
/// <param name="httpClientFactory">Creates the named finder <see cref="HttpClient"/> (base address from <see cref="FinderClientOptions"/>).</param>
/// <param name="tokens">Provides the bearer tokens.</param>
/// <param name="logger">Logger of this client.</param>
public sealed class FinderApiClient(IHttpClientFactory httpClientFactory, FinderTokenProvider tokens, ILogger<FinderApiClient> logger)
{
    private const int CopyBufferSize = 81920;

    /// <summary><c>POST /api/v1/Search/Document</c> – search document folders by name, ID, full text, additionals, ….</summary>
    public Task<DocumentSearchResult> SearchDocumentsAsync(DocumentSearchRequest request, CancellationToken ct = default) =>
        SendJsonAsync<DocumentSearchResult>(HttpMethod.Post, "api/v1/Search/Document", request, ct);

    /// <summary>
    /// <c>POST /api/v1/Search/Uploaded</c> – finds an uploaded document by the <c>temporaryName</c> returned by
    /// the upload. Returns <c>null</c> (HTTP 204) as long as the document has not been processed yet.
    /// </summary>
    public async Task<UploadedSearchHit?> FindUploadedDocumentAsync(string temporaryName, CancellationToken ct = default)
    {
        var body = new UploadedSearchRequest { Query = temporaryName };
        using var response = await SendAsync(HttpMethod.Post, "api/v1/Search/Uploaded", body, ct: ct).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return null;
        }

        var result = await response.ReadJsonAsync<UploadedSearchResult>(ct).ConfigureAwait(false);
        return result.Hits?.FirstOrDefault();
    }

    /// <summary><c>POST /api/v1/Category/Process</c> – process steps the user can add documents to.</summary>
    public Task<List<Category>> GetProcessStepsAsync(CancellationToken ct = default) =>
        SendJsonAsync<List<Category>>(HttpMethod.Post, "api/v1/Category/Process", null, ct);

    /// <summary><c>POST /api/v1/Category/Archive</c> – archive categories the user can add documents to.</summary>
    public Task<List<Category>> GetArchiveCategoriesAsync(CancellationToken ct = default) =>
        SendJsonAsync<List<Category>>(HttpMethod.Post, "api/v1/Category/Archive", null, ct);

    public Task<List<Category>> GetLocationsAsync(LocationKind kind, CancellationToken ct = default) =>
        kind == LocationKind.ProcessStep ? GetProcessStepsAsync(ct) : GetArchiveCategoriesAsync(ct);

    /// <summary><c>POST /api/v1/Category/Additionals</c> – additionals available in the given locations.</summary>
    public Task<List<CategoryAdditional>> GetCategoryAdditionalsAsync(IEnumerable<int> categoryIds, CancellationToken ct = default)
    {
        var body = new CategoryAdditionalsRequest { Id = categoryIds.ToList() };
        return SendJsonAsync<List<CategoryAdditional>>(HttpMethod.Post, "api/v1/Category/Additionals", body, ct);
    }

    /// <summary><c>GET /api/v2/additional/select/{id}</c> – options of a <c>select</c> additional (10 per page, page starts at 1).</summary>
    public Task<AdditionalSelection> GetSelectOptionsAsync(int additionalId, string? search = null, int page = 1, CancellationToken ct = default)
    {
        var query = $"page={page}";
        if (!string.IsNullOrWhiteSpace(search))
        {
            query += $"&search={Uri.EscapeDataString(search)}";
        }

        return SendJsonAsync<AdditionalSelection>(HttpMethod.Get, $"api/v2/additional/select/{additionalId}?{query}", null, ct);
    }

    /// <summary><c>GET /api/v2/additional/suggestion/{id}</c> – existing values of a <c>string</c> additional containing <paramref name="term"/> (max. 50).</summary>
    public Task<List<string>> GetSuggestionsAsync(int additionalId, string term, CancellationToken ct = default) =>
        SendJsonAsync<List<string>>(HttpMethod.Get, $"api/v2/additional/suggestion/{additionalId}?term={Uri.EscapeDataString(term)}", null, ct);

    /// <summary>
    /// <c>POST /api/v2/Additional/Validate</c> – validates additional values against the target locations.
    /// Returns the IDs of the invalid additionals (empty when everything is valid).
    /// </summary>
    public async Task<IReadOnlyList<long>> ValidateAdditionalsAsync(AdditionalValidationRequest request, CancellationToken ct = default)
    {
        using var response = await SendAsync(HttpMethod.Post, "api/v2/Additional/Validate", request, ensureSuccess: false, ct: ct).ConfigureAwait(false);
        if (response.IsSuccessStatusCode)
        {
            return [];
        }

        // Documented: 400 with a JSON array of the invalid IDs. A problem details 400 (malformed request) or any
        // other status is an error, not a validation result.
        if (response.StatusCode != HttpStatusCode.BadRequest || response.HasMediaType(MediaTypes.ProblemJson))
        {
            throw await FinderApiException.FromResponseAsync(response, ct).ConfigureAwait(false);
        }

        return await response.ReadJsonAsync<List<long>>(ct).ConfigureAwait(false);
    }

    /// <summary><c>GET /api/v2/Upload/Rights</c> – what the current user may set on upload.</summary>
    public Task<UploadRights> GetUploadRightsAsync(CancellationToken ct = default) =>
        SendJsonAsync<UploadRights>(HttpMethod.Get, "api/v2/Upload/Rights", null, ct);

    /// <summary><c>GET /api/v2/Document/{efinGuid}</c> – metadata and documents of a folder.</summary>
    public Task<Folder> GetFolderAsync(string folderId, CancellationToken ct = default) =>
        SendJsonAsync<Folder>(HttpMethod.Get, $"api/v2/Document/{Uri.EscapeDataString(folderId)}", null, ct);

    /// <summary><c>GET /api/v2/Document/File/{efinGuid}/content</c> – recognized text of one page of a document.</summary>
    public async Task<string> GetDocumentTextAsync(string documentId, int page = 1, CancellationToken ct = default)
    {
        var path = $"api/v2/Document/File/{Uri.EscapeDataString(documentId)}/content?page={page}";
        using var response = await SendAsync(HttpMethod.Get, path, ct: ct).ConfigureAwait(false);
        return await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// <c>GET /api/v2/media/{file}</c> – downloads a document into <paramref name="targetDirectory"/> using the
    /// original file name from the <c>Content-Disposition</c> header. Requires the scope <c>finder.api.media</c>.
    /// </summary>
    /// <param name="documentId">EfinGuid of the document (not the folder), e.g. <see cref="DocumentSearchHit.CurrentFileId"/>.</param>
    public async Task<DownloadedFile> DownloadDocumentAsync(string documentId, string targetDirectory, CancellationToken ct = default)
    {
        using var response = await SendAsync(
            HttpMethod.Get,
            $"api/v2/media/{Uri.EscapeDataString(documentId)}",
            scope: FinderTokenProvider.MediaScope,
            completion: HttpCompletionOption.ResponseHeadersRead,
            ct: ct).ConfigureAwait(false);

        Directory.CreateDirectory(targetDirectory);
        var fileName = SanitizeFileName(GetFileName(response.Content.Headers.ContentDisposition) ?? documentId);
        var path = GetUniquePath(Path.Combine(targetDirectory, fileName));

        await using var source = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        await using var target = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, CopyBufferSize, useAsync: true);
        await source.CopyToAsync(target, CopyBufferSize, ct).ConfigureAwait(false);

        logger.DocumentDownloaded(documentId, path, target.Length);
        return new DownloadedFile(path, target.Length, response.Content.Headers.ContentType?.MediaType);
    }

    internal static string SanitizeFileName(string name)
    {
        name = Path.GetFileName(name.Replace('\\', '/'));
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }

        return string.IsNullOrWhiteSpace(name) || name is "." or ".." ? "download" : name;
    }

    private static HttpContent? CreateContent(HttpMethod method, object? body) => body switch
    {
        not null => JsonContent.Create(body, body.GetType(), options: FinderJson.Options),

        // The v1 category endpoints are POSTs without a body.
        null when method == HttpMethod.Post => new ByteArrayContent([]),
        _ => null,
    };

    private static string? GetFileName(ContentDispositionHeaderValue? disposition)
    {
        var name = disposition?.FileNameStar ?? disposition?.FileName;
        return string.IsNullOrWhiteSpace(name) ? null : name.Trim('"');
    }

    private static string GetUniquePath(string path)
    {
        if (!File.Exists(path))
        {
            return path;
        }

        var dir = Path.GetDirectoryName(path)!;
        var stem = Path.GetFileNameWithoutExtension(path);
        var ext = Path.GetExtension(path);
        for (var i = 1; ; i++)
        {
            var candidate = Path.Combine(dir, $"{stem} ({i}){ext}");
            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }
    }

    private async Task<T> SendJsonAsync<T>(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        using var response = await SendAsync(method, path, body, ct: ct).ConfigureAwait(false);
        return await response.ReadJsonAsync<T>(ct).ConfigureAwait(false);
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string path,
        object? body = null,
        string scope = FinderTokenProvider.ReadWriteScope,
        HttpCompletionOption completion = HttpCompletionOption.ResponseContentRead,
        bool ensureSuccess = true,
        CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(method, path) { Content = CreateContent(method, body) };
        var http = httpClientFactory.CreateClient(ServiceCollectionExtensions.HttpClientName);
        return await http.SendAuthorizedAsync(tokens, request, scope, completion, ensureSuccess, ct).ConfigureAwait(false);
    }
}
