using System.Text.Json.Serialization;

namespace FinderDemo.Models.Uploads;

/// <summary>Returned (HTTP 201) once the last fragment was received and the document was accepted.</summary>
public sealed class UploadSessionCompleted
{
    /// <summary>Reference to check the processing state via <c>POST /api/v1/Search/Uploaded</c>.</summary>
    [JsonPropertyName("temporaryName")]
    public string TemporaryName { get; init; } = string.Empty;

    /// <summary>MD5 (hex) of the received file as computed by the server.</summary>
    [JsonPropertyName("contentMD5")]
    public string? ContentMd5 { get; init; }
}
