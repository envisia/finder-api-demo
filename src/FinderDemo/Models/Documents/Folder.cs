using System.Text.Json.Serialization;

namespace FinderDemo.Models.Documents;

/// <summary>Response of <c>GET /api/v2/Document/{efinGuid}</c>: a document folder ("Dokumenten Mappe").</summary>
public sealed class Folder
{
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("date")]
    public DateTimeOffset? Date { get; init; }

    /// <summary>The documents (file versions) inside the folder.</summary>
    [JsonPropertyName("documents")]
    public List<FolderDocument>? Documents { get; init; }

    [JsonPropertyName("deletedDocuments")]
    public List<FolderDocument>? DeletedDocuments { get; init; }

    [JsonPropertyName("partner")]
    public BusinessPartner? Partner { get; init; }
}
