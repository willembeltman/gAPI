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

public static class SendRequestDoneDtoMultipartFormDataContentSerializer
{

    [IsMultipartFormDataContentSerializer]
    public static void Write(this MultipartFormDataContent ___content, string ___name, SendRequestDoneDto value)
    {
        RequestIdMultipartFormDataContentSerializer.Write(___content, "RequestId", value.RequestId);
        ServiceIdMultipartFormDataContentSerializer.Write(___content, "ServiceId", value.ServiceId);
        ServiceMethodIdMultipartFormDataContentSerializer.Write(___content, "MethodId", value.MethodId);
        if (value.UserId != null)
            UserIdMultipartFormDataContentSerializer.Write(___content, "UserId", value.UserId.Value);
        if (value.SessionId != null)
            SessionIdMultipartFormDataContentSerializer.Write(___content, "SessionId", value.SessionId.Value);
        ___content.Add(new StringContent(value.StateIsChanged.ToString()), "StateIsChanged");
        if (value.StateData != null)
            ___content.Add(new StringContent(value.StateData), "StateData");
        if (value.ExceptionMessage != null)
            ___content.Add(new StringContent(value.ExceptionMessage), "ExceptionMessage");
    }
}