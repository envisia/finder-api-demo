using FinderDemo.Services;

namespace FinderDemo.Models.Uploads;

/// <summary>Progress of <see cref="FinderUploadClient.UploadAsync"/>.</summary>
public sealed record UploadProgress(UploadStage Stage, long BytesSent, long TotalBytes, string? SessionKey = null)
{
    public double Fraction => TotalBytes == 0 ? 0 : (double)BytesSent / TotalBytes;
}
