namespace FinderDemo.Services;

internal static class MediaTypes
{
    public const string Json = "application/json";

    /// <summary>RFC 9457 problem details, used by the API for error responses.</summary>
    public const string ProblemJson = "application/problem+json";

    public const string OctetStream = "application/octet-stream";
}
