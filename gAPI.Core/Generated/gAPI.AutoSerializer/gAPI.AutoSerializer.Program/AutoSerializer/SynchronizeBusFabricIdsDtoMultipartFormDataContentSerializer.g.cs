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

public static class SynchronizeBusFabricIdsDtoMultipartFormDataContentSerializer
{

    [IsMultipartFormDataContentSerializer]
    public static void Write(this MultipartFormDataContent ___content, string ___name, SynchronizeBusFabricIdsDto value)
    {
        string ___prefix = string.IsNullOrEmpty(___name) ? "" : $"{___name}.";
        
        FabricManagerIdMultipartFormDataContentSerializer.Write(___content, $"{___prefix}FabricManagerId", value.FabricManagerId);
        FabricBusConnectionIdMultipartFormDataContentSerializer.Write(___content, $"{___prefix}FabricConnectionId", value.FabricConnectionId);
    }
}