using System.Text.Json.Serialization;

namespace FinderDemo.Models.Uploads;

/// <summary>
/// Request body of <c>POST /api/v2/CreateUploadSession</c>: creates an upload session for a new document
/// folder. Location, additionals and other metadata are fixed here, the file content follows in fragments.
/// </summary>
public sealed record CreateUploadSessionRequest
{
    /// <summary>File name including extension, e.g. <c>Abrechnung.pdf</c>.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Target location IDs: exactly one process step, or one or more archive categories.
    /// </summary>
    [JsonPropertyName("categories")]
    public required IReadOnlyList<int> Categories { get; init; }

    /// <summary>
    /// CRC-32C of the whole file (Base64 of the big-endian uint32), checked when the last fragment arrives.
    /// If both hashes are set, the server uses this one.
    /// </summary>
    [JsonPropertyName("crc32cHash")]
    public string? Crc32cHash { get; init; }

    /// <summary>Hex encoded MD5 of the whole file, checked when the last fragment arrives. Default of <see cref="Services.FinderUploadClient"/>.</summary>
    [JsonPropertyName("md5Hash")]
    public string? Md5Hash { get; init; }

    [JsonPropertyName("additionals")]
    public IReadOnlyList<UploadSessionAdditional>? Additionals { get; init; }

    /// <summary>Additional keywords ("Zusätzliche Schlagworte"); only if the location allows them.</summary>
    [JsonPropertyName("additionalWords")]
    public string? AdditionalWords { get; init; }

    /// <summary>File numbers ("Aktenzeichen"); only if <see cref="UploadRights.AddFileNumber"/> is granted.</summary>
    [JsonPropertyName("fileNumbers")]
    public IReadOnlyList<string>? FileNumbers { get; init; }

    [JsonPropertyName("separatedByBlankPages")]
    public bool? SeparatedByBlankPages { get; init; }

    [JsonPropertyName("removeBlankPages")]
    public bool? RemoveBlankPages { get; init; }

    [JsonPropertyName("addToLastUsedCategories")]
    public bool? AddToLastUsedCategories { get; init; }

    /// <summary>Try to extract contained files (e.g. attachments of an EML) and process them separately.</summary>
    [JsonPropertyName("extract")]
    public bool? Extract { get; init; }

    [JsonPropertyName("pinned")]
    public bool? Pinned { get; init; }

    [JsonPropertyName("referenceId")]
    public string? ReferenceId { get; init; }
}
