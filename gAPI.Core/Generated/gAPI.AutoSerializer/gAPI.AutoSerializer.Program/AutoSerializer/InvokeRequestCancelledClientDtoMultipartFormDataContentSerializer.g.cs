using gAPI.Core.Attributes;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Dtos;
using gAPI.Core.Serializers;
using System;
using System.Buffers.Binary;
using System.Text;

#nullable enable
namespace gAPI.Core.Dtos;

public static class InvokeRequestCancelledClientDtoMultipartFormDataContentSerializer
{

    [IsMultipartFormDataContentSerializer]
    public static void Write(this MultipartFormDataContent ___content, string ___name, InvokeRequestCancelledClientDto value)
    {
        RoutingDtoMultipartFormDataContentSerializer.Write(___content, "Routing", value.Routing);
        if (value.Reason != null)
            ___content.Add(new StringContent(value.Reason), "Reason");
        ___content.Add(new StringContent(value.StateIsChanged.ToString()), "StateIsChanged");
        if (value.StateData != null)
            ___content.Add(new StringContent(value.StateData), "StateData");
    }
}