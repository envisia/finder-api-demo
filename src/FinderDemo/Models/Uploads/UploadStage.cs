namespace FinderDemo.Models.Uploads;

public enum UploadStage
{
    Hashing,
    CreatingSession,
    Uploading,
    Retrying,
    Completed,
}
