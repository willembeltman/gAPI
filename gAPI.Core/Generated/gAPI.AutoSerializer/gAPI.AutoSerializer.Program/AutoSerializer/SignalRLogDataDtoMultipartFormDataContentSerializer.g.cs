using gAPI.Core.Attributes;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Serializers;
using gAPI.Core.Wss;
using System;
using System.Buffers.Binary;
using System.Text;

#nullable enable
namespace gAPI.Core.Wss;

public static class SignalRLogDataDtoMultipartFormDataContentSerializer
{

    [IsMultipartFormDataContentSerializer]
    public static void Write(this MultipartFormDataContent ___content, string ___name, SignalRLogDataDto value)
    {
        ___content.Add(new StringContent(value.Key), "Key");
        if (value.Value != null)
            ___content.Add(new StringContent(value.Value), "Value");
    }
}