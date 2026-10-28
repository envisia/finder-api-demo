namespace FinderDemo.Models.Uploads;

/// <summary>Result of uploading a single fragment: either the server expects more bytes, or the upload is complete.</summary>
public sealed record FragmentResult(UploadSessionState? Pending, UploadSessionCompleted? Completed);
