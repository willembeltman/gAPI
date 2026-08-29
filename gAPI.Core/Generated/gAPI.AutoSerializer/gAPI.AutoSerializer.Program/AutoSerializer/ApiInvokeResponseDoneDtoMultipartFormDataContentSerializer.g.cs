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

public static class ApiInvokeResponseDoneDtoMultipartFormDataContentSerializer
{

    [IsMultipartFormDataContentSerializer]
    public static void Write(this MultipartFormDataContent ___content, string ___name, ApiInvokeResponseDoneDto value)
    {
        RequestIdMultipartFormDataContentSerializer.Write(___content, "RequestId", value.RequestId);
        ServiceIdMultipartFormDataContentSerializer.Write(___content, "ServiceId", value.ServiceId);
        ServiceMethodIdMultipartFormDataContentSerializer.Write(___content, "MethodId", value.MethodId);
        if (value.SessionData != null)
            ___content.Add(new StringContent(value.SessionData), "SessionData");
        if (value.StateData != null)
            ___content.Add(new StringContent(value.StateData), "StateData");
    }
}