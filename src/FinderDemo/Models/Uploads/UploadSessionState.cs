using System.Text.Json.Serialization;

namespace FinderDemo.Models.Uploads;

/// <summary>
/// State of an upload session (<c>FileUploadSessionChunkResponse</c>). Returned when the session is created,
/// for every accepted fragment (HTTP 202) and by the status endpoint.
/// </summary>
public sealed class UploadSessionState
{
    /// <summary>Session key used in <c>/api/v2/FileUploadSession/{key}</c>.</summary>
    [JsonPropertyName("key")]
    public string Key { get; init; } = string.Empty;

    /// <summary>Byte ranges the server still expects, e.g. <c>["8388608-"]</c>.</summary>
    [JsonPropertyName("nextExpectedRanges")]
    public List<string> NextExpectedRanges { get; init; } = [];

    [JsonPropertyName("expirationDateTime")]
    public DateTimeOffset ExpirationDateTime { get; init; }
}
