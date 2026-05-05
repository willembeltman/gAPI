using gAPI.Core.AttributesSerializers;
using System.Buffers.Binary;

namespace gAPI.Core.Serializers;

public static class DateTimeOffsetSerializers
{
    [IsSerializerRead]
    public static DateTimeOffset ReadDateTimeOffset(this BinaryReader br)
    {
        long ticks = br.ReadInt64();
        int offsetMinutes = br.ReadInt32();
        return new DateTimeOffset(ticks, TimeSpan.FromMinutes(offsetMinutes));
    }
    [IsSerializerWrite]
    public static void Write(this BinaryWriter bw, DateTimeOffset value)
    {
        bw.Write(value.Ticks);              // long
        bw.Write((int)value.Offset.TotalMinutes); // int
    }
    [IsSpanSerializerRead]
    public static DateTimeOffset ReadDateTimeOffset(this ReadOnlySpan<byte> span, ref int offset)
    {
        var ticks = BinaryPrimitives.ReadInt64LittleEndian(span.Slice(offset, 8));
        offset += 8;
        var offsetMinutes = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(offset, 4));
        offset += 4;
        return new DateTimeOffset(ticks, TimeSpan.FromMinutes(offsetMinutes));
    }
    [IsSpanSerializerWrite]
    public static void Write(this ref Span<byte> span, ref int offset, DateTimeOffset value)
    {
        BinaryPrimitives.WriteInt64LittleEndian(span.Slice(offset, 8), value.Ticks);
        offset += 8;
        BinaryPrimitives.WriteInt32LittleEndian(span.Slice(offset, 4), (int)value.Offset.TotalMinutes);
        offset += 4;
    }
    [IsSpanSerializerLength]
    public static int Length(ref int offset, DateTimeOffset value)
    {
        offset += 8;
        offset += 4;
        return offset;
    }

}
