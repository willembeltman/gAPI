using gAPI.Core.AttributesSerializers;
using System.Buffers.Binary;

namespace gAPI.Core.Serializers;

public static class ByteArraySerializer
{
    [IsSerializerRead]
    public static byte[] ReadByteArray(this BinaryReader br)
    {
        var len = br.ReadInt32();
        return br.ReadBytes(len);
    }
    [IsSerializerWrite]
    public static void Write(this BinaryWriter bw, byte[] value)
    {
        bw.Write(value.Length);
        bw.Write(value);
    }

    [IsSpanSerializerRead]
    public static byte[] ReadByteArray(this ReadOnlySpan<byte> span, ref int offset)
    {
        var len = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(offset, 4));
        offset += len + 4;
        return span.Slice(offset, len).ToArray();
    }
    [IsSpanSerializerWrite]
    public static void Write(this ref Span<byte> span, ref int offset, byte[] value)
    {
        BinaryPrimitives.WriteInt32LittleEndian(span.Slice(offset, 4), value.Length);
        offset += 4;
        value.CopyTo(span.Slice(offset, value.Length));
    }

    [IsSpanSerializerLength]
    public static int Length(ref int offset, byte[] value)
    {
        offset += 4 + value.Length;
        return offset;
    }

    [IsMultipartFormDataContentSerializer]
    public static void Write(this MultipartFormDataContent content, string name, byte[] value)
    {
        // Harde cap op 1gb
        if (value.Length > 1024 * 1024 * 1024)
            throw new Exception("Array too big to send");

        string prefix = string.IsNullOrEmpty(name) ? "" : $"{name}.";

        if (value != null)
        {
            var base64String = Convert.ToBase64String(value);
            content.Add(new StringContent(base64String), $"{prefix}BinaryData");
        }
    }
}
