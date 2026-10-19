using gAPI.Core.AttributesSerializers;
using System.Buffers.Binary;

namespace gAPI.Core.Serializers;

public static class TimeSpanSerializer
{
    [IsSpanSerializerRead]
    public static TimeSpan ReadTimeSpan(this ReadOnlySpan<byte> span, ref int offset)
    {
        var value = BinaryPrimitives.ReadInt64LittleEndian(span.Slice(offset, 8));
        offset += 8;
        return TimeSpan.FromTicks(value);
    }

    [IsSpanSerializerWrite]
    public static void Write(this ref Span<byte> span, ref int offset, TimeSpan value)
    {
        BinaryPrimitives.WriteInt64LittleEndian(span.Slice(offset, 8), value.Ticks);
        offset += 8;
    }

    [IsSerializerRead]
    public static TimeSpan ReadTimeSpan(this BinaryReader br)
    {
        return TimeSpan.FromTicks(br.ReadInt64());
    }

    [IsSerializerWrite]
    public static void Write(this BinaryWriter bw, TimeSpan value)
    {
        bw.Write(value.Ticks);
    }

    [IsSpanSerializerLength]
    public static int Length(ref int offset, TimeSpan value)
    {
        offset += 8;
        return offset;
    }
}
