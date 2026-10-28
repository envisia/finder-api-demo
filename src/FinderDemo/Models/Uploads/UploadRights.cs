using System.Text.Json.Serialization;

namespace FinderDemo.Models.Uploads;

/// <summary>Response of <c>GET /api/v2/Upload/Rights</c>.</summary>
public sealed class UploadRights
{
    /// <summary>The user may set file numbers ("Aktenzeichen") on upload.</summary>
    [JsonPropertyName("addFileNumber")]
    public bool AddFileNumber { get; init; }
}
