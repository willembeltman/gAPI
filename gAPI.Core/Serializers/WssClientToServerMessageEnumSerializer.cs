using gAPI.Core.AttributesSerializers;
using gAPI.Core.Enums;

namespace gAPI.Core.Serializers;

public static class WssClientToServerMessageEnumSerializer
{
    [IsSpanSerializerWrite]
    public static void WriteWssClientToServerMessageEnum(this ref Span<byte> span, ref int offset, WssClientToServerMessageEnum value)
    {
        PrimitivesSpanSerializer.WriteInt32(ref span, ref offset, (int)value);
    }
    [IsSpanSerializerRead]
    public static WssClientToServerMessageEnum ReadWssClientToServerMessageEnum(this ReadOnlySpan<byte> span, ref int offset)
    {
        return (WssClientToServerMessageEnum)PrimitivesSpanSerializer.ReadInt32(span, ref offset);
    }
    [IsSpanSerializerLength]
    public static int GetMessageLength(ref int offset, WssClientToServerMessageEnum value)
    {
        offset += 4;
        return offset;
    }
}
