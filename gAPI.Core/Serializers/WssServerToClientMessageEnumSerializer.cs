using gAPI.AttributesSerializers;
using gAPI.Enums;

namespace gAPI.Serializers;

public static class WssServerToClientMessageEnumSerializer
{
    [IsSpanSerializerWrite]
    public static void WriteWssServerToClientMessageEnum(this ref Span<byte> span, ref int offset, WssServerToClientMessageEnum value)
    {
        PrimitivesSpanSerializer.WriteInt32(ref span, ref offset, (int)value);
    }
    [IsSpanSerializerRead]
    public static WssServerToClientMessageEnum ReadWssServerToClientMessageEnum(this ReadOnlySpan<byte> span, ref int offset)
    {
        return (WssServerToClientMessageEnum)PrimitivesSpanSerializer.ReadInt32(span, ref offset);
    }
    [IsSpanSerializerLength]
    public static int GetMessageLength(ref int offset, WssServerToClientMessageEnum value)
    {
        offset += 4;
        return offset;
    }
}
