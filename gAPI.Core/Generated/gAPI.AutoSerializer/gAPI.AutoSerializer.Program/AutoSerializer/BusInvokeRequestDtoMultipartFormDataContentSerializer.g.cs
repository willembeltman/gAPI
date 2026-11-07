using gAPI.Core.Attributes;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Dtos;
using gAPI.Core.Serializers;
using System;
using System.Buffers.Binary;
using System.Text;

#nullable enable
namespace gAPI.Core.Dtos;

public static class BusInvokeRequestDtoMultipartFormDataContentSerializer
{

    [IsMultipartFormDataContentSerializer]
    public static void Write(this MultipartFormDataContent ___content, string ___name, BusInvokeRequestDto value)
    {
        string ___prefix = string.IsNullOrEmpty(___name) ? "" : $"{___name}.";
        
        BusRoutingDtoMultipartFormDataContentSerializer.Write(___content, $"{___prefix}Routing", value.Routing);
        ___content.Add(new ByteArrayContent(value.BinaryData), $"{___prefix}BinaryData", "file");
    }
}