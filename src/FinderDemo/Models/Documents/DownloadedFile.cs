using FinderDemo.Services;

namespace FinderDemo.Models.Documents;

/// <summary>A file that was written to disk by <see cref="FinderApiClient.DownloadDocumentAsync"/>.</summary>
public sealed record DownloadedFile(string Path, long Length, string? ContentType);
