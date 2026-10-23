using gAPI.Core.Attributes;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Dtos;
using gAPI.Core.Serializers;
using System;
using System.Buffers.Binary;
using System.Text;

#nullable enable
namespace gAPI.Core.Dtos;

public static class SendRequestClientDtoMultipartFormDataContentSerializer
{

    [IsMultipartFormDataContentSerializer]
    public static void Write(this MultipartFormDataContent ___content, string ___name, SendRequestClientDto value)
    {
        string ___prefix = string.IsNullOrEmpty(___name) ? "" : $"{___name}.";
        
        RoutingDtoMultipartFormDataContentSerializer.Write(___content, $"{___prefix}Routing", value.Routing);
        ___content.Add(new ByteArrayContent(value.BinaryData), $"{___prefix}BinaryData", "file");
        ___content.Add(new StringContent(value.StateIsChanged.ToString()), $"{___prefix}StateIsChanged");
        if (value.StateData != null)
            ___content.Add(new StringContent(value.StateData), $"{___prefix}StateData");
    }
}