using gAPI.Core.Attributes;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Dtos;
using gAPI.Core.Serializers;
using System;
using System.Buffers.Binary;
using System.Text;

#nullable enable
namespace gAPI.Core.Dtos;

public static class InvokeRequestDoneClientDtoMultipartFormDataContentSerializer
{

    [IsMultipartFormDataContentSerializer]
    public static void Write(this MultipartFormDataContent ___content, string ___name, InvokeRequestDoneClientDto value)
    {
        string ___prefix = string.IsNullOrEmpty(___name) ? "" : $"{___name}.";
        
        RoutingDtoMultipartFormDataContentSerializer.Write(___content, $"{___prefix}Routing", value.Routing);
        ___content.Add(new StringContent(value.Cancelled.ToString()), $"{___prefix}Cancelled");
        if (value.ExceptionMessage != null)
            ___content.Add(new StringContent(value.ExceptionMessage), $"{___prefix}ExceptionMessage");
        ___content.Add(new StringContent(value.StateIsChanged.ToString()), $"{___prefix}StateIsChanged");
        if (value.StateData != null)
            ___content.Add(new StringContent(value.StateData), $"{___prefix}StateData");
    }
}