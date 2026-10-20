using gAPI.Core.Attributes;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Dtos;
using gAPI.Core.Ids;
using gAPI.Core.Serializers;
using System;
using System.Buffers.Binary;
using System.Text;

#nullable enable
namespace gAPI.Core.Dtos;

public static class StreamingResponseDtoMultipartFormDataContentSerializer
{

    [IsMultipartFormDataContentSerializer]
    public static void Write(this MultipartFormDataContent ___content, string ___name, StreamingResponseDto value)
    {
        string ___prefix = string.IsNullOrEmpty(___name) ? "" : $"{___name}.";
        
        RoutingDtoMultipartFormDataContentSerializer.Write(___content, $"{___prefix}Routing", value.Routing);
        ___content.Add(new StringContent(value.ArgumentIndex.ToString()), $"{___prefix}ArgumentIndex");
        StreamIdMultipartFormDataContentSerializer.Write(___content, $"{___prefix}StreamId", value.StreamId);
        ___content.Add(new StringContent(value.IsCompleted.ToString()), $"{___prefix}IsCompleted");
        ___content.Add(new StringContent(value.IsCancelled.ToString()), $"{___prefix}IsCancelled");
        if (value.ExceptionMessage != null)
            ___content.Add(new StringContent(value.ExceptionMessage), $"{___prefix}ExceptionMessage");
        ___content.Add(new ByteArrayContent(value.BinaryData), $"{___prefix}BinaryData", "file");
    }
}