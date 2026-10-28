using System.Buffers.Binary;
using System.Numerics;

namespace FinderDemo.Services;

/// <summary>
/// CRC-32C (Castagnoli) as expected by <c>CreateUploadSessionRequest.crc32cHash</c>: the checksum of the whole
/// file, encoded as Base64 of the big-endian uint32 (e.g. <c>"AjpDJg=="</c>).
/// Uses <see cref="BitOperations.Crc32C(uint, ulong)"/>, which is hardware accelerated on x64 (SSE4.2) and ARM64.
/// </summary>
public static class Crc32C
{
    private const uint InitialState = 0xFFFFFFFF;

    public static uint Compute(ReadOnlySpan<byte> data) => ~Append(InitialState, data);

    public static async Task<uint> ComputeAsync(Stream stream, CancellationToken ct = default)
    {
        var buffer = new byte[1024 * 1024];
        var state = InitialState;
        int read;
        while ((read = await stream.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
        {
            state = Append(state, buffer.AsSpan(0, read));
        }

        return ~state;
    }

    /// <summary>Base64 of the big-endian representation, the format used by the API.</summary>
    public static string ToBase64(uint crc)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, crc);
        return Convert.ToBase64String(bytes);
    }

    private static uint Append(uint state, ReadOnlySpan<byte> data)
    {
        while (data.Length >= sizeof(ulong))
        {
            state = BitOperations.Crc32C(state, BinaryPrimitives.ReadUInt64LittleEndian(data));
            data = data[sizeof(ulong)..];
        }

        foreach (var b in data)
        {
            state = BitOperations.Crc32C(state, b);
        }

        return state;
    }
}
