using gAPI.Core.AttributesSerializers;
using System.Buffers.Binary;

namespace gAPI.Core.Serializers;

public static class DateTimeSerializers
{
    [IsSpanSerializerRead]
    public static DateTime ReadDateTime(this ReadOnlySpan<byte> span, ref int offset)
    {
        var value = BinaryPrimitives.ReadInt64LittleEndian(span.Slice(offset, 8));
        offset += 8;
        return DateTime.FromBinary(value);
    }
    [IsSpanSerializerWrite]
    public static void Write(this ref Span<byte> span, ref int offset, DateTime value)
    {
        BinaryPrimitives.WriteInt64LittleEndian(span.Slice(offset, 8), value.ToBinary());
        offset += 8;
    }
    [IsSerializerRead]
    public static DateTime ReadDateTime(this BinaryReader br)
    {
        return DateTime.FromBinary(br.ReadInt64());
    }
    [IsSerializerWrite]
    public static void Write(this BinaryWriter bw, DateTime value)
    {
        bw.Write(value.ToBinary());
    }
    [IsSpanSerializerLength]
    public static int Length(ref int offset, DateTime value)
    {
        offset += 8;
        return offset;
    }
}
