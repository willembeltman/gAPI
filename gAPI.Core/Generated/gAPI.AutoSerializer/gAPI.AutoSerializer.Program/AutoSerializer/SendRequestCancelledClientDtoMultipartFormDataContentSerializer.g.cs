using gAPI.Core.Attributes;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Dtos;
using gAPI.Core.Serializers;
using System;
using System.Buffers.Binary;
using System.Text;

#nullable enable
namespace gAPI.Core.Dtos;

public static class SendRequestCancelledClientDtoMultipartFormDataContentSerializer
{

    [IsMultipartFormDataContentSerializer]
    public static void Write(this MultipartFormDataContent ___content, string ___name, SendRequestCancelledClientDto value)
    {
        string ___prefix = string.IsNullOrEmpty(___name) ? "" : $"{___name}.";
        
        RoutingDtoMultipartFormDataContentSerializer.Write(___content, $"{___prefix}Routing", value.Routing);
        if (value.Reason != null)
            ___content.Add(new StringContent(value.Reason), $"{___prefix}Reason");
        ___content.Add(new StringContent(value.StateIsChanged.ToString()), $"{___prefix}StateIsChanged");
        if (value.StateData != null)
            ___content.Add(new StringContent(value.StateData), $"{___prefix}StateData");
    }
}