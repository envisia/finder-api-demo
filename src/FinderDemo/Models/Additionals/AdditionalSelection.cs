using System.Text.Json.Serialization;

namespace FinderDemo.Models.Additionals;

/// <summary>Response of <c>GET /api/v2/additional/select/{id}</c> (max. 10 entries per page).</summary>
public sealed class AdditionalSelection
{
    [JsonPropertyName("entryValues")]
    public List<string>? EntryValues { get; init; }

    [JsonPropertyName("hasNext")]
    public bool HasNext { get; init; }
}
