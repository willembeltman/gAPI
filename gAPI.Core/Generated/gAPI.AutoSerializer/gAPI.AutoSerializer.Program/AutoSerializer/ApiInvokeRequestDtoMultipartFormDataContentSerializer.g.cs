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

public static class ApiInvokeRequestDtoMultipartFormDataContentSerializer
{

    [IsMultipartFormDataContentSerializer]
    public static void Write(this MultipartFormDataContent ___content, string ___name, ApiInvokeRequestDto value)
    {
        RequestIdMultipartFormDataContentSerializer.Write(___content, "RequestId", value.RequestId);
        ServiceIdMultipartFormDataContentSerializer.Write(___content, "ServiceId", value.ServiceId);
        ServiceMethodIdMultipartFormDataContentSerializer.Write(___content, "MethodId", value.MethodId);
        if (value.SessionId != null)
            SessionIdMultipartFormDataContentSerializer.Write(___content, "SessionId", value.SessionId.Value);
        if (value.StateData != null)
            ___content.Add(new StringContent(value.StateData), "StateData");
        ___content.Add(new ByteArrayContent(value.BinaryData), "BinaryData", "file");
    }
}