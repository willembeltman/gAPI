using System.Buffers.Binary;
using System.Text;

namespace gAPI.Core.Serializers;

public static class PrimitivesSpanSerializer
{
    public static void WriteInt32(ref Span<byte> span, ref int offset, int value)
    {
        BinaryPrimitives.WriteInt32LittleEndian(span.Slice(offset, 4), value);
        offset += 4;
    }
    public static int ReadInt32(ReadOnlySpan<byte> span, ref int offset)
    {
        var value = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(offset, 4));
        offset += 4;
        return value;
    }
    public static void LengthInt32(ref int offset, int value)
    {
        offset += 4;
    }

    public static void WriteInt64(ref Span<byte> span, ref int offset, long value)
    {
        BinaryPrimitives.WriteInt64LittleEndian(span.Slice(offset, 8), value);
        offset += 8;
    }
    public static long ReadInt64(ReadOnlySpan<byte> span, ref int offset)
    {
        var value = BinaryPrimitives.ReadInt64LittleEndian(span.Slice(offset, 8));
        offset += 8;
        return value;
    }
    public static void LengthInt64(ref int offset, long value)
    {
        offset += 8;
    }

    public static void WriteBoolean(ref Span<byte> span, ref int offset, bool value)
    {
        span[offset++] = value ? (byte)1 : (byte)0;
    }
    public static bool ReadBoolean(ReadOnlySpan<byte> span, ref int offset)
    {
        return span[offset++] == 1;
    }
    public static void LengthBoolean(ref int offset, bool value)
    {
        offset++;
    }

    public static void WriteString(ref Span<byte> span, ref int offset, string value)
    {
        var byteCount = Encoding.UTF8.GetByteCount(value);
        WriteInt32(ref span, ref offset, byteCount);
        Encoding.UTF8.GetBytes(value, span.Slice(offset, byteCount));
        offset += byteCount;
    }
    public static string ReadString(ReadOnlySpan<byte> span, ref int offset)
    {
        var len = ReadInt32(span, ref offset);
        var s = Encoding.UTF8.GetString(span.Slice(offset, len));
        offset += len;
        return s;
    }
    public static void LengthString(ref int offset, string value)
    {
        var byteCount = Encoding.UTF8.GetByteCount(value);
        LengthInt32(ref offset, byteCount);
        offset += byteCount;
    }

    public static void WriteByteArray(ref Span<byte> span, ref int offset, byte[] value)
    {
        var byteCount = value.Length;
        WriteInt32(ref span, ref offset, byteCount);
        value.CopyTo(span.Slice(offset, byteCount));
        offset += byteCount;
    }
    public static byte[] ReadByteArray(ReadOnlySpan<byte> span, ref int offset)
    {
        var len = ReadInt32(span, ref offset);
        var s = span.Slice(offset, len).ToArray();
        offset += len;
        return s;
    }
    public static void LengthByteArray(ref int offset, byte[] value)
    {
        var byteCount = value.Length;
        LengthInt32(ref offset, byteCount);
        offset += byteCount;
    }

    public static void WriteUShort(ref Span<byte> span, ref int offset, ushort value)
    {
        span[offset++] = (byte)(value & 0xFF);       // low byte
        span[offset++] = (byte)(value >> 8);         // high byte
    }
    public static ushort ReadUShort(ReadOnlySpan<byte> span, ref int offset)
    {
        ushort value = (ushort)(span[offset] | (span[offset + 1] << 8));
        offset += 2;
        return value;
    }
    public static void LengthUShort(ref int offset, ushort value)
    {
        offset += 2;
    }

    public static void WriteUInt(ref Span<byte> span, ref int offset, uint value)
    {
        span[offset++] = (byte)(value & 0xFF);         // byte 0
        span[offset++] = (byte)((value >> 8) & 0xFF);  // byte 1
        span[offset++] = (byte)((value >> 16) & 0xFF); // byte 2
        span[offset++] = (byte)((value >> 24) & 0xFF); // byte 3
    }
    public static uint ReadUInt(ReadOnlySpan<byte> span, ref int offset)
    {
        uint value = (uint)(
            span[offset] |
            (span[offset + 1] << 8) |
            (span[offset + 2] << 16) |
            (span[offset + 3] << 24)
        );
        offset += 4;
        return value;
    }
    public static void LengthUInt(ref int offset, uint value)
    {
        offset += 4;
    }

    public static void WriteSingle(ref Span<byte> span, ref int offset, float value)
    {
        int bits = BitConverter.SingleToInt32Bits(value);
        BinaryPrimitives.WriteInt32LittleEndian(span.Slice(offset, 4), bits);
        offset += 4;
    }
    public static float ReadSingle(Span<byte> span, ref int offset)
    {
        int bits = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(offset, 4));
        offset += 4;
        return BitConverter.Int32BitsToSingle(bits);
    }
    public static void LengthSingle(ref int offset, float value)
    {
        offset += 4;
    }
}
