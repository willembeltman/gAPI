using gAPI.Core.Attributes;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Dtos;
using gAPI.Core.Serializers;
using System;
using System.Buffers.Binary;
using System.Text;

#nullable enable
namespace gAPI.Core.Dtos;

public static class ServerSendRequestCancelledDtoMultipartFormDataContentSerializer
{

    [IsMultipartFormDataContentSerializer]
    public static void Write(this MultipartFormDataContent ___content, string ___name, ServerSendRequestCancelledDto value)
    {
        string ___prefix = string.IsNullOrEmpty(___name) ? "" : $"{___name}.";
        
        ServerRoutingDtoMultipartFormDataContentSerializer.Write(___content, $"{___prefix}Routing", value.Routing);
        if (value.Reason != null)
            ___content.Add(new StringContent(value.Reason), $"{___prefix}Reason");
    }
}