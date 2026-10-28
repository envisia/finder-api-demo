using System.Text.Json;
using System.Text.Json.Serialization;

namespace FinderDemo.Services;

internal static class FinderJson
{
    /// <summary>
    /// Every model declares its wire name with <c>[JsonPropertyName]</c> (the API mixes snake_case in v1 and
    /// camelCase in v2). <c>null</c> properties are omitted so optional filters are simply not sent.
    /// </summary>
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>Same as <see cref="Options"/>, indented – used to show request bodies in the console.</summary>
    public static readonly JsonSerializerOptions Indented = new(Options) { WriteIndented = true };
}
