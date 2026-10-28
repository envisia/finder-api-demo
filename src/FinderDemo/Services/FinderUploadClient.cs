using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using FinderDemo.Extensions;
using FinderDemo.Logging;
using FinderDemo.Models.Uploads;
using Microsoft.Extensions.Logging;

namespace FinderDemo.Services;

/// <summary>
/// Implements the envisia finder upload session protocol (API v2):
/// <list type="number">
///   <item><c>POST /api/v2/CreateUploadSession</c> with name, location(s), additionals and checksum → session key.</item>
///   <item><c>PUT /api/v2/FileUploadSession/{key}</c> once per fragment with a <c>Content-Range</c> header.
///         The server answers <c>202</c> with the next expected range, and <c>201</c> with a <c>temporaryName</c> after the last fragment.</item>
///   <item>On errors the session state can be queried (<c>GET</c>) to resume, or the session is deleted (<c>DELETE</c>).</item>
/// </list>
/// Fragment rules: max. 20 MiB, recommended ~8 MiB, every fragment except the last must be a multiple of 256 KiB,
/// and the last fragment must be smaller than the others.
/// </summary>
public sealed class FinderUploadClient(IHttpClientFactory httpClientFactory, ILogger<FinderUploadClient> logger)
{
    public const int FragmentAlignment = 256 * 1024;
    public const int MaxFragmentSize = 20 * 1024 * 1024;
    public const int RecommendedFragmentSize = 8 * 1024 * 1024;

    private const int MaxAttemptsPerFragment = 3;
    private const int FileBufferSize = 81920;

    private static readonly TimeSpan DeleteTimeout = TimeSpan.FromSeconds(10);

    /// <summary>Base delay before retrying a failed fragment (multiplied by the attempt number).</summary>
    private static readonly TimeSpan RetryBaseDelay = TimeSpan.FromSeconds(2);

    /// <summary><c>POST /api/v2/CreateUploadSession</c>.</summary>
    public async Task<UploadSessionState> CreateUploadSessionAsync(CreateUploadSessionRequest request, CancellationToken ct = default)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "api/v2/CreateUploadSession")
        {
            Content = JsonContent.Create(request, options: FinderJson.Options),
        };
        using var response = await SendAsync(message, ct).ConfigureAwait(false);
        return await response.ReadJsonAsync<UploadSessionState>(ct).ConfigureAwait(false);
    }

    /// <summary><c>GET /api/v2/FileUploadSession/{key}</c> – which bytes the server still expects.</summary>
    public async Task<UploadSessionState> GetUploadSessionAsync(string key, CancellationToken ct = default)
    {
        using var message = new HttpRequestMessage(HttpMethod.Get, SessionPath(key));
        using var response = await SendAsync(message, ct).ConfigureAwait(false);
        return await response.ReadJsonAsync<UploadSessionState>(ct).ConfigureAwait(false);
    }

    /// <summary><c>DELETE /api/v2/FileUploadSession/{key}</c> – discards the session and all uploaded fragments.</summary>
    public async Task DeleteUploadSessionAsync(string key, CancellationToken ct = default)
    {
        using var message = new HttpRequestMessage(HttpMethod.Delete, SessionPath(key));
        using var response = await SendAsync(message, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// <c>PUT /api/v2/FileUploadSession/{key}</c> with <c>Content-Range: bytes {first}-{last}/{total}</c>.
    /// The total size must be identical for all fragments of a session.
    /// </summary>
    public async Task<FragmentResult> UploadFragmentAsync(string key, ReadOnlyMemory<byte> fragment, long offset, long totalLength, CancellationToken ct = default)
    {
        // Content-Length is set automatically from the fragment size.
        var content = new ReadOnlyMemoryContent(fragment);
        content.Headers.ContentType = new MediaTypeHeaderValue(MediaTypes.OctetStream);
        content.Headers.ContentRange = new ContentRangeHeaderValue(offset, offset + fragment.Length - 1, totalLength);

        using var message = new HttpRequestMessage(HttpMethod.Put, SessionPath(key)) { Content = content };
        using var response = await SendAsync(message, ct).ConfigureAwait(false);

        return response.StatusCode switch
        {
            HttpStatusCode.Created => new FragmentResult(null, await response.ReadJsonAsync<UploadSessionCompleted>(ct).ConfigureAwait(false)),
            HttpStatusCode.Accepted => new FragmentResult(await response.ReadJsonAsync<UploadSessionState>(ct).ConfigureAwait(false), null),
            _ => throw new FinderApiException($"Unexpected status {(int)response.StatusCode} for an upload fragment.", response.StatusCode),
        };
    }

    /// <summary>Uploads a file from disk as a new document folder. See <see cref="UploadAsync"/>.</summary>
    public async Task<UploadSessionCompleted> UploadFileAsync(
        string path,
        CreateUploadSessionRequest request,
        IProgress<UploadProgress>? progress = null,
        UploadChecksum checksum = UploadChecksum.Md5,
        CancellationToken ct = default)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, FileBufferSize, useAsync: true);
        return await UploadAsync(stream, request, progress, checksum, RecommendedFragmentSize, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Complete upload: computes the checksum (unless the request already contains one), creates the session and
    /// uploads all fragments. Transient errors are retried by asking the server which range it expects next.
    /// If the upload fails for good or is canceled, the session is deleted.
    /// </summary>
    /// <param name="content">A seekable stream with the file content.</param>
    /// <param name="request">Name, target locations, additionals and options of the new document folder.</param>
    /// <param name="progress">Receives the current stage and the number of bytes the server has acknowledged.</param>
    /// <param name="checksum">Which checksum to send: MD5 (<c>md5Hash</c>, default) or CRC-32C (<c>crc32cHash</c>).</param>
    /// <param name="preferredFragmentSize">Fragment size, a multiple of 256 KiB up to 20 MiB.</param>
    /// <param name="ct">Cancels the upload; the session is deleted.</param>
    /// <returns>The <c>temporaryName</c>; pass it to <see cref="FinderApiClient.FindUploadedDocumentAsync"/> to find the processed folder.</returns>
    public async Task<UploadSessionCompleted> UploadAsync(
        Stream content,
        CreateUploadSessionRequest request,
        IProgress<UploadProgress>? progress = null,
        UploadChecksum checksum = UploadChecksum.Md5,
        int preferredFragmentSize = RecommendedFragmentSize,
        CancellationToken ct = default)
    {
        if (!content.CanSeek)
        {
            throw new ArgumentException("The upload stream must be seekable.", nameof(content));
        }

        var total = content.Length;
        if (total == 0)
        {
            throw new ArgumentException("Empty files cannot be uploaded.", nameof(content));
        }

        if (request.Crc32cHash is null && request.Md5Hash is null)
        {
            progress?.Report(new UploadProgress(UploadStage.Hashing, 0, total));
            content.Position = 0;
            request = checksum switch
            {
                UploadChecksum.Crc32C => request with { Crc32cHash = Crc32C.ToBase64(await Crc32C.ComputeAsync(content, ct).ConfigureAwait(false)) },
                _ => request with { Md5Hash = await ComputeMd5HexAsync(content, ct).ConfigureAwait(false) },
            };
        }

        progress?.Report(new UploadProgress(UploadStage.CreatingSession, 0, total));
        var session = await CreateUploadSessionAsync(request, ct).ConfigureAwait(false);

        // The server uses the CRC-32C if both hashes are set.
        logger.UploadSessionCreated(session.Key, request.Name, total, request.Crc32cHash is not null ? UploadChecksum.Crc32C : UploadChecksum.Md5);

        try
        {
            return await UploadFragmentsAsync(content, session, preferredFragmentSize, progress, ct).ConfigureAwait(false);
        }
        catch
        {
            await TryDeleteSessionAsync(session.Key).ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Picks the fragment size for a file: the preferred size (a multiple of 256 KiB), reduced in 256 KiB steps
    /// if the file length is an exact multiple of it – otherwise the last fragment would be as large as the
    /// others, but the API requires it to be smaller.
    /// </summary>
    public static int ChooseFragmentSize(long totalLength, int preferredSize = RecommendedFragmentSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(totalLength, 0);
        if (preferredSize <= 0 || preferredSize > MaxFragmentSize || preferredSize % FragmentAlignment != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(preferredSize), $"Must be a multiple of {FragmentAlignment} bytes and at most {MaxFragmentSize} bytes.");
        }

        // Fits into a single fragment.
        if (totalLength <= preferredSize)
        {
            return preferredSize;
        }

        var size = preferredSize;
        while (totalLength % size == 0 && size > FragmentAlignment)
        {
            size -= FragmentAlignment;
        }

        return size;
    }

    /// <summary>Parses the start offset of the first range in <c>nextExpectedRanges</c> (<c>"8388608-"</c> or <c>"8388608-16777215"</c>).</summary>
    public static long? ParseNextOffset(IReadOnlyList<string>? ranges)
    {
        var first = ranges?.FirstOrDefault(r => !string.IsNullOrWhiteSpace(r));
        if (first is null)
        {
            return null;
        }

        var dash = first.IndexOf('-');
        var start = dash < 0 ? first : first[..dash];
        return long.TryParse(start.Trim(), out var value) ? value : null;
    }

    /// <summary>MD5 as lower case hex string, the format of <c>md5Hash</c> (e.g. <c>b4194f5960fe9fe240ad82e3d11e7db3</c>).</summary>
    /// <remarks>MD5 is used as transfer checksum here, as defined by the API, not for security.</remarks>
    public static async Task<string> ComputeMd5HexAsync(Stream content, CancellationToken ct = default) =>
        Convert.ToHexStringLower(await MD5.HashDataAsync(content, ct).ConfigureAwait(false));

    private static bool IsTransient(Exception e, CancellationToken ct) => e switch
    {
        HttpRequestException => true,

        // HttpClient timeout (not our own cancellation).
        TaskCanceledException when !ct.IsCancellationRequested => true,
        FinderApiException { StatusCode: >= HttpStatusCode.InternalServerError } => true,
        FinderApiException { StatusCode: HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests } => true,
        _ => false,
    };

    private static string SessionPath(string key) => $"api/v2/FileUploadSession/{Uri.EscapeDataString(key)}";

    /// <summary>
    /// The first byte the server still expects. It has to lie inside the file – anything else would make the next
    /// fragment empty or negative, so it ends the upload with a clear error instead.
    /// </summary>
    private static long ExpectedOffset(IReadOnlyList<string> ranges, long total)
    {
        var offset = ParseNextOffset(ranges) ?? throw new FinderApiException("The server did not report the next expected byte range.");
        if (offset == total)
        {
            throw new FinderApiException($"The server already has all {total} bytes but did not confirm the upload.");
        }

        return offset >= 0 && offset < total
            ? offset
            : throw new FinderApiException($"The server expects bytes from {offset}, which is outside the file ({total} bytes).");
    }

    private async Task<UploadSessionCompleted> UploadFragmentsAsync(
        Stream content,
        UploadSessionState session,
        int preferredFragmentSize,
        IProgress<UploadProgress>? progress,
        CancellationToken ct)
    {
        var key = session.Key;
        var total = content.Length;
        var fragmentSize = ChooseFragmentSize(total, preferredFragmentSize);
        var buffer = new byte[Math.Min(fragmentSize, total)];
        var offset = session.NextExpectedRanges.Count == 0 ? 0 : ExpectedOffset(session.NextExpectedRanges, total);
        var attempt = 0;

        while (true)
        {
            progress?.Report(new UploadProgress(UploadStage.Uploading, offset, total, key));

            var count = (int)Math.Min(fragmentSize, total - offset);
            content.Position = offset;
            await content.ReadExactlyAsync(buffer.AsMemory(0, count), ct).ConfigureAwait(false);
            logger.UploadingFragment(offset, offset + count - 1, total, key);

            FragmentResult result;
            try
            {
                result = await UploadFragmentAsync(key, buffer.AsMemory(0, count), offset, total, ct).ConfigureAwait(false);
                attempt = 0;
            }
            catch (Exception e) when (IsTransient(e, ct) && ++attempt < MaxAttemptsPerFragment)
            {
                // Resume: ask the server which bytes it is missing and continue from there.
                logger.FragmentFailed(e, offset, key, attempt);
                progress?.Report(new UploadProgress(UploadStage.Retrying, offset, total, key));
                await Task.Delay(RetryBaseDelay * attempt, ct).ConfigureAwait(false);
                var state = await GetUploadSessionAsync(key, ct).ConfigureAwait(false);
                offset = ExpectedOffset(state.NextExpectedRanges, total);
                continue;
            }

            if (result.Completed is { } completed)
            {
                logger.UploadCompleted(key, completed.TemporaryName);
                progress?.Report(new UploadProgress(UploadStage.Completed, total, total, key));
                return completed;
            }

            // The server has to move forward after accepting a fragment, otherwise the loop would never end.
            var next = ExpectedOffset(result.Pending!.NextExpectedRanges, total);
            if (next <= offset)
            {
                throw new FinderApiException($"The server accepted bytes {offset}–{offset + count - 1} but still expects bytes from {next}.");
            }

            offset = next;
        }
    }

    /// <summary>Best effort cleanup after a failed upload; the original exception is what matters to the caller.</summary>
    private async Task TryDeleteSessionAsync(string key)
    {
        try
        {
            using var cts = new CancellationTokenSource(DeleteTimeout);
            await DeleteUploadSessionAsync(key, cts.Token).ConfigureAwait(false);
            logger.UploadSessionDeleted(key);
        }
        catch (Exception e) when (e is HttpRequestException or FinderApiException or OperationCanceledException)
        {
            // The DELETE can fail for the same reason the upload did; the session then simply expires on the
            // server (see expirationDateTime).
            logger.UploadSessionDeleteFailed(e, key);
        }
    }

    /// <summary>Sends with the finder <see cref="HttpClient"/>, whose <see cref="FinderAuthorizationHandler"/> adds the bearer token.</summary>
    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage message, CancellationToken ct)
    {
        var response = await httpClientFactory.CreateClient(ServiceCollectionExtensions.HttpClientName)
            .SendAsync(message, ct)
            .ConfigureAwait(false);
        return await response.EnsureFinderSuccessAsync(ct).ConfigureAwait(false);
    }
}
