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

public static class SynchronizeClientIdsDtoMultipartFormDataContentSerializer
{

    [IsMultipartFormDataContentSerializer]
    public static void Write(this MultipartFormDataContent ___content, string ___name, SynchronizeClientIdsDto value)
    {
        FabricManagerIdMultipartFormDataContentSerializer.Write(___content, "FabricManagerId", value.FabricManagerId);
        FabricConnectionIdMultipartFormDataContentSerializer.Write(___content, "FabricConnectionId", value.FabricConnectionId);
        ClientConnectionIdMultipartFormDataContentSerializer.Write(___content, "ClientConnectionId", value.ClientConnectionId);
    }
}