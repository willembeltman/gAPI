using gAPI.Core.Attributes;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Serializers;
using gAPI.Core.Wss;
using System;
using System.Buffers.Binary;
using System.Text;

#nullable enable
namespace gAPI.Core.Wss;

public static class WssLoggerLogDtoMultipartFormDataContentSerializer
{

    [IsMultipartFormDataContentSerializer]
    public static void Write(this MultipartFormDataContent ___content, string ___name, WssLoggerLogDto value)
    {
        string ___prefix = string.IsNullOrEmpty(___name) ? "" : $"{___name}.";
        
        ___content.Add(new StringContent(((int)value.Level).ToString()), $"{___prefix}Level");
        ___content.Add(new StringContent(value.Message), $"{___prefix}Message");
        if (value.Category != null)
            ___content.Add(new StringContent(value.Category), $"{___prefix}Category");
        if (value.Source != null)
            ___content.Add(new StringContent(value.Source), $"{___prefix}Source");
        ___content.Add(new StringContent(value.Timestamp.ToString("O")), $"{___prefix}Timestamp");
        if (value.CorrelationId != null)
            ___content.Add(new StringContent(value.CorrelationId), $"{___prefix}CorrelationId");
        if (value.UserId != null)
            ___content.Add(new StringContent(value.UserId), $"{___prefix}UserId");
        if (value.StackTrace != null)
            ___content.Add(new StringContent(value.StackTrace), $"{___prefix}StackTrace");
    }
}