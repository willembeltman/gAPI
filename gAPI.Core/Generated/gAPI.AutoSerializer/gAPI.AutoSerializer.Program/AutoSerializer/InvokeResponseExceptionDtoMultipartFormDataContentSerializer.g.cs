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

public static class InvokeResponseExceptionDtoMultipartFormDataContentSerializer
{

    [IsMultipartFormDataContentSerializer]
    public static void Write(this MultipartFormDataContent ___content, string ___name, InvokeResponseExceptionDto value)
    {
        RequestIdMultipartFormDataContentSerializer.Write(___content, "RequestId", value.RequestId);
        ServiceIdMultipartFormDataContentSerializer.Write(___content, "ServiceId", value.ServiceId);
        ServiceMethodIdMultipartFormDataContentSerializer.Write(___content, "MethodId", value.MethodId);
        if (value.SessionId != null)
            SessionIdMultipartFormDataContentSerializer.Write(___content, "SessionId", value.SessionId.Value);
        if (value.UserId != null)
            UserIdMultipartFormDataContentSerializer.Write(___content, "UserId", value.UserId.Value);
        ___content.Add(new StringContent(value.ExceptionMessage), "ExceptionMessage");
    }
}