using System.Text.Json;
using System.Text.Json.Serialization;

namespace gAPI.Llm.Client;

public static class DefaultJsonSerializerOptions
{
    public static JsonSerializerOptions JsonSerializeOptionsIndented = new JsonSerializerOptions
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };
    public static JsonSerializerOptions JsonSerializeOptions = new JsonSerializerOptions
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    public static JsonSerializerOptions JsonDeserializerOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };
}

