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

public static class ServerStreamingRequestDtoMultipartFormDataContentSerializer
{

    [IsMultipartFormDataContentSerializer]
    public static void Write(this MultipartFormDataContent ___content, string ___name, ServerStreamingRequestDto value)
    {
        string ___prefix = string.IsNullOrEmpty(___name) ? "" : $"{___name}.";
        
        ServerRoutingDtoMultipartFormDataContentSerializer.Write(___content, $"{___prefix}Routing", value.Routing);
        ___content.Add(new StringContent(value.ArgumentIndex.ToString()), $"{___prefix}ArgumentIndex");
        StreamIdMultipartFormDataContentSerializer.Write(___content, $"{___prefix}StreamId", value.StreamId);
        ___content.Add(new StringContent(value.Cancelled.ToString()), $"{___prefix}Cancelled");
    }
}