using System.Text.Json.Serialization;

namespace FinderDemo.Models.Documents;

public sealed class BusinessPartner
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("number")]
    public string? Number { get; init; }
}
