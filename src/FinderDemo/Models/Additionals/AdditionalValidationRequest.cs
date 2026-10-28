using System.Text.Json.Serialization;
using FinderDemo.Models.Uploads;

namespace FinderDemo.Models.Additionals;

/// <summary>Request body of <c>POST /api/v2/Additional/Validate</c>.</summary>
public sealed class AdditionalValidationRequest
{
    [JsonPropertyName("categories")]
    public required IReadOnlyList<int> Categories { get; init; }

    [JsonPropertyName("additionals")]
    public IReadOnlyList<UploadSessionAdditional>? Additionals { get; init; }
}
