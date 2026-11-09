using gAPI.Core.Attributes;
using gAPI.Core.AttributesSerializers;

namespace gAPI.Core.Serializers;

public static class GuidSerializer
{
    [IsSpanSerializerRead]
    public static Guid ReadGuid(ReadOnlySpan<byte> span, ref int offset, int maxBufferSize = 16)
    {
        var value = new Guid(span.Slice(offset, 16));
        offset += 16;
        return value;
    }

    [IsSpanSerializerWrite]
    public static void WriteGuid(ref Span<byte> span, ref int offset, Guid value)
    {
        value.TryWriteBytes(span.Slice(offset, 16));
        offset += 16;
    }

    [IsSerializerRead]
    public static Guid ReadGuid(this BinaryReader br, int maxBufferSize = 16)
    {
        return new Guid(br.ReadBytes(16));
    }

    [IsSerializerWrite]
    public static void Write(this BinaryWriter bw, Guid value)
    {
        bw.Write(value.ToByteArray());
    }

    [IsComparer]
    public static bool IsDifferent(this Guid value, Guid otherValue)
    {
        return value.Equals(otherValue) == false;
    }
    [IsSpanSerializerLength]
    public static int GetMessageLength(ref int offset, Guid value)
    {
        offset += 16;
        return offset;
    }
    [IsMultipartFormDataContentSerializer]
    public static void Write(this MultipartFormDataContent content, string propertyName, Guid value)
    {
        content.Add(new StringContent(value.ToString()), propertyName);
    }
}
