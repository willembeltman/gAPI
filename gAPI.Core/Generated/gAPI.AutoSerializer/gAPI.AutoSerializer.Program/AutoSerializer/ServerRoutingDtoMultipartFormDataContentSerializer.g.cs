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

public static class ServerRoutingDtoMultipartFormDataContentSerializer
{

    [IsMultipartFormDataContentSerializer]
    public static void Write(this MultipartFormDataContent ___content, string ___name, ServerRoutingDto value)
    {
        string ___prefix = string.IsNullOrEmpty(___name) ? "" : $"{___name}.";
        
        FabricManagerIdMultipartFormDataContentSerializer.Write(___content, $"{___prefix}From_FabricManagerId", value.From_FabricManagerId);
        FabricConnectionIdMultipartFormDataContentSerializer.Write(___content, $"{___prefix}From_FabricConnectionId", value.From_FabricConnectionId);
        RequestIdMultipartFormDataContentSerializer.Write(___content, $"{___prefix}RequestId", value.RequestId);
        ServiceIdMultipartFormDataContentSerializer.Write(___content, $"{___prefix}ServiceId", value.ServiceId);
        ServiceMethodIdMultipartFormDataContentSerializer.Write(___content, $"{___prefix}MethodId", value.MethodId);
        FabricConnectionIdMultipartFormDataContentSerializer.Write(___content, $"{___prefix}FabricConnectionId", value.FabricConnectionId);
        ClientConnectionIdMultipartFormDataContentSerializer.Write(___content, $"{___prefix}ClientConnectionId", value.ClientConnectionId);
    }
}