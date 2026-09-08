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
        RoutingDtoMultipartFormDataContentSerializer.Write(___content, "Routing", value.Routing);
        ___content.Add(new StringContent(value.Cancelled.ToString()), "Cancelled");
        if (value.ExceptionMessage != null)
            ___content.Add(new StringContent(value.ExceptionMessage), "ExceptionMessage");
        ___content.Add(new StringContent(value.StateIsChanged.ToString()), "StateIsChanged");
        if (value.StateData != null)
            ___content.Add(new StringContent(value.StateData), "StateData");
    }
}