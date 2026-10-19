using gAPI.Core.Attributes;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Serializers;
using System;
using System.Buffers.Binary;
using System.Text;

#nullable enable
namespace System;

public static class TimeSpanMultipartFormDataContentSerializer
{

    [IsMultipartFormDataContentSerializer]
    public static void Write(this MultipartFormDataContent ___content, string ___name, TimeSpan value)
    {
        string ___prefix = string.IsNullOrEmpty(___name) ? "" : $"{___name}.";
        
    }
}