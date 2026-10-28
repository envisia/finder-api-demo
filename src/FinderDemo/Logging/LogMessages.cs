using FinderDemo.Models.Uploads;
using Microsoft.Extensions.Logging;

namespace FinderDemo.Logging;

/// <summary>
/// Source generated log messages: services 1000–1999, areas 2000–2999. None of them takes a token, a client secret,
/// a header or a body as parameter.
/// </summary>
internal static partial class LogMessages
{
    [LoggerMessage(EventId = 1000, Message = "HTTP {Method} {Path} -> {StatusCode} in {ElapsedMs} ms")]
    public static partial void HttpCallCompleted(this ILogger logger, LogLevel level, string method, string path, int statusCode, long elapsedMs);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Error, Message = "HTTP {Method} {Path} failed after {ElapsedMs} ms")]
    public static partial void HttpCallFailed(this ILogger logger, Exception exception, string method, string path, long elapsedMs);

    [LoggerMessage(EventId = 1100, Level = LogLevel.Debug, Message = "Requesting an access token ({GrantType}) for scope {Scope}")]
    public static partial void RequestingToken(this ILogger logger, string grantType, string scope);

    [LoggerMessage(EventId = 1101, Level = LogLevel.Information, Message = "Received an access token for scope {Scope}, valid for {ExpiresIn} s")]
    public static partial void ReceivedToken(this ILogger logger, string scope, int expiresIn);

    [LoggerMessage(EventId = 1102, Level = LogLevel.Warning, Message = "Token request for scope {Scope} failed with {StatusCode}: {Error}")]
    public static partial void TokenRequestFailed(this ILogger logger, string scope, int statusCode, string? error);

    [LoggerMessage(EventId = 1200, Level = LogLevel.Information, Message = "Downloaded document {DocumentId} to {Path} ({Length} bytes)")]
    public static partial void DocumentDownloaded(this ILogger logger, string documentId, string path, long length);

    [LoggerMessage(EventId = 1300, Level = LogLevel.Information, Message = "Created upload session {SessionKey} for {FileName} ({TotalBytes} bytes, {Checksum} checksum)")]
    public static partial void UploadSessionCreated(this ILogger logger, string sessionKey, string fileName, long totalBytes, UploadChecksum checksum);

    [LoggerMessage(EventId = 1301, Level = LogLevel.Debug, Message = "Uploading bytes {First}-{Last}/{TotalBytes} to session {SessionKey}")]
    public static partial void UploadingFragment(this ILogger logger, long first, long last, long totalBytes, string sessionKey);

    [LoggerMessage(EventId = 1302, Level = LogLevel.Warning, Message = "Fragment at offset {Offset} of session {SessionKey} failed (attempt {Attempt}); asking the server where to resume")]
    public static partial void FragmentFailed(this ILogger logger, Exception exception, long offset, string sessionKey, int attempt);

    [LoggerMessage(EventId = 1303, Level = LogLevel.Information, Message = "Upload session {SessionKey} completed, temporaryName {TemporaryName}")]
    public static partial void UploadCompleted(this ILogger logger, string sessionKey, string temporaryName);

    [LoggerMessage(EventId = 1304, Level = LogLevel.Information, Message = "Deleted upload session {SessionKey} after the upload failed or was canceled")]
    public static partial void UploadSessionDeleted(this ILogger logger, string sessionKey);

    [LoggerMessage(EventId = 1305, Level = LogLevel.Warning, Message = "Could not delete upload session {SessionKey}; it expires on the server")]
    public static partial void UploadSessionDeleteFailed(this ILogger logger, Exception exception, string sessionKey);

    [LoggerMessage(EventId = 2000, Level = LogLevel.Information, Message = "finder-demo started for {Server} (client {ClientId}); downloads go to {DownloadDirectory}")]
    public static partial void Started(this ILogger logger, Uri server, string clientId, string downloadDirectory);

    [LoggerMessage(EventId = 2001, Level = LogLevel.Information, Message = "finder-demo stopped")]
    public static partial void Stopped(this ILogger logger);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Critical, Message = "finder-demo terminated unexpectedly")]
    public static partial void Crashed(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Error, Message = "{Action} failed")]
    public static partial void ActionFailed(this ILogger logger, Exception exception, string action);

    [LoggerMessage(EventId = 2100, Level = LogLevel.Information, Message = "Search found {TotalHits} folder(s)")]
    public static partial void SearchCompleted(this ILogger logger, long totalHits);

    [LoggerMessage(EventId = 2200, Level = LogLevel.Warning, Message = "Server rejected additionals {AdditionalIds}")]
    public static partial void AdditionalsRejected(this ILogger logger, IReadOnlyList<long> additionalIds);

    [LoggerMessage(EventId = 2201, Level = LogLevel.Information, Message = "Uploaded document processed: folder {FolderId}")]
    public static partial void UploadProcessed(this ILogger logger, string folderId);

    [LoggerMessage(EventId = 2202, Level = LogLevel.Warning, Message = "Uploaded document {TemporaryName} not processed within {Timeout}")]
    public static partial void UploadNotProcessed(this ILogger logger, string temporaryName, TimeSpan timeout);
}
