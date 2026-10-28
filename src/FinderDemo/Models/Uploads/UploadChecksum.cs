namespace FinderDemo.Models.Uploads;

/// <summary>Checksum sent with <c>CreateUploadSession</c>; the server verifies it when the last fragment arrives.</summary>
public enum UploadChecksum
{
    /// <summary><c>md5Hash</c>: hex encoded MD5 of the file (default).</summary>
    Md5,

    /// <summary><c>crc32cHash</c>: Base64 of the big-endian CRC-32C of the file.</summary>
    Crc32C,
}
