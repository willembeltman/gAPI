using gAPI.Core.Attributes;
using gAPI.Core.AttributesSerializers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace gAPI.Core.Serializers;

public static class Dictionary_String_Object_Serializer
{
    [IsSpanSerializerWrite]
    public static void WriteDictionary_String_Object(ref Span<byte> span, ref int offset, Dictionary<string, object> value)
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
    public static Dictionary<string, object> ReadDictionary_String_Object(ReadOnlySpan<byte> span, ref int offset)
    {
        // 1. Lees de lengte van de JSON string
        int length = BitConverter.ToInt32(span.Slice(offset, 4));
        offset += 4;

        if (length == -1) return null;

        // 2. Lees de JSON bytes uit de span en deserialiseer
        ReadOnlySpan<byte> jsonSpan = span.Slice(offset, length);
        offset += length;

        return JsonSerializer.Deserialize<Dictionary<string, object>>(jsonSpan) ?? new Dictionary<string, object>();
    }

    [IsSerializerRead]
    public static Dictionary<string, object> ReadDictionary_String_Object(this BinaryReader br)
    {
        int length = br.ReadInt32();
        if (length == -1) return null;

        byte[] jsonBytes = br.ReadBytes(length);
        return JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes) ?? new Dictionary<string, object>();
    }

    [IsSerializerWrite]
    public static void Write(this BinaryWriter bw, Dictionary<string, object> value)
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
    public static bool IsDifferent(this Dictionary<string, object> value, Dictionary<string, object> otherValue)
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

    [IsSpanSerializerLength]
    public static int GetMessageLength(ref int offset, Dictionary<string, object> value)
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
    public static void Write(this MultipartFormDataContent content, string propertyName, Dictionary<string, object> value)
    {
        string jsonString = value != null ? JsonSerializer.Serialize(value) : "{}";
        content.Add(new StringContent(jsonString, Encoding.UTF8, "application/json"), propertyName);
    }
}
