using gAPI.Core.Attributes;
using gAPI.Core.AttributesSerializers;
using System.Text;
using System.Text.Json;

namespace gAPI.Core.Serializers;

public static class Dictionary_String_String_Serializer
{
    [IsSpanSerializerWrite]
    public static void WriteDictionary_String_String(ref Span<byte> span, ref int offset, Dictionary<string, string> value)
    {
        if (value == null)
        {
            BitConverter.TryWriteBytes(span.Slice(offset, 4), -1);
            offset += 4;
            return;
        }

        // 1. Serialize naar JSON bytes
        byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(value);
        int length = jsonBytes.Length;

        // 2. Schrijf de lengte van de byte-array weg (4 bytes int)
        BitConverter.TryWriteBytes(span.Slice(offset, 4), length);
        offset += 4;

        // 3. Schrijf de daadwerkelijke JSON bytes in de span
        jsonBytes.CopyTo(span.Slice(offset, length));
        offset += length;
    }

    [IsSpanSerializerRead]
    public static Dictionary<string, string> ReadDictionary_String_String(ReadOnlySpan<byte> span, ref int offset, int maxBufferSize)
    {
        // 1. Lees de lengte van de JSON string
        int length = BitConverter.ToInt32(span.Slice(offset, 4));
        offset += 4;

        // 2. Lees de JSON bytes uit de span en deserialiseer
        ReadOnlySpan<byte> jsonSpan = span.Slice(offset, length);
        offset += length;

        return JsonSerializer.Deserialize<Dictionary<string, string>>(jsonSpan) ?? new Dictionary<string, string>();
    }

    [IsSerializerRead]
    public static Dictionary<string, string> ReadDictionary_String_String(this BinaryReader br, int maxBufferSize)
    {
        int length = br.ReadInt32();
        byte[] jsonBytes = br.ReadBytes(length);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(jsonBytes) ?? new Dictionary<string, string>();
    }

    [IsSerializerWrite]
    public static void Write(this BinaryWriter bw, Dictionary<string, string> value)
    {
        if (value == null)
        {
            bw.Write(-1);
            return;
        }

        byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(value);
        bw.Write(jsonBytes.Length);
        bw.Write(jsonBytes);
    }

    [IsComparer]
    public static bool IsDifferent(this Dictionary<string, string> value, Dictionary<string, string> otherValue)
    {
        if (ReferenceEquals(value, otherValue)) return false;
        if (value == null || otherValue == null) return true;
        if (value.Count != otherValue.Count) return true;

        // Omdat waarden in de dictionary primitives, strings of JsonElements kunnen zijn,
        // is een JSON-string vergelijking hier de meest veilige en waterdichte fallback.
        string json1 = JsonSerializer.Serialize(value);
        string json2 = JsonSerializer.Serialize(otherValue);
        return json1 != json2;
    }


    [IsCreateCopy]
    public static Dictionary<string, string> CreateCopy(this Dictionary<string, string> value)
    {
        return new Dictionary<string, string>(value);
    }


    [IsSpanSerializerLength]
    public static int GetMessageLength(ref int offset, Dictionary<string, string> value)
    {
        if (value == null)
        {
            offset += 4; // Alleen de -1 int lengte indicator
            return offset;
        }

        byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(value);
        offset += 4 + jsonBytes.Length; // 4 bytes voor de lengte + de JSON data zelf
        return offset;
    }

    [IsMultipartFormDataContentSerializer]
    public static void Write(this MultipartFormDataContent content, string propertyName, Dictionary<string, string> value)
    {
        string jsonString = value != null ? JsonSerializer.Serialize(value) : "{}";
        content.Add(new StringContent(jsonString, Encoding.UTF8, "application/json"), propertyName);
    }
}
