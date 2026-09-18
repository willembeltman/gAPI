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
        ___content.Add(new StringContent(((int)value.Level).ToString()), "Level");
        ___content.Add(new StringContent(value.Message), "Message");
        if (value.Category != null)
            ___content.Add(new StringContent(value.Category), "Category");
        if (value.Source != null)
            ___content.Add(new StringContent(value.Source), "Source");
        ___content.Add(new StringContent(value.Timestamp.ToString("O")), "Timestamp");
        if (value.CorrelationId != null)
            ___content.Add(new StringContent(value.CorrelationId), "CorrelationId");
        if (value.UserId != null)
            ___content.Add(new StringContent(value.UserId), "UserId");
        if (value.Data != null)
        {
            foreach (var item1 in value.Data)
            {

                SignalRLogDataDtoMultipartFormDataContentSerializer.Write(___content, "Data", item1);
            }
        }
        if (value.StackTrace != null)
            ___content.Add(new StringContent(value.StackTrace), "StackTrace");
    }
}