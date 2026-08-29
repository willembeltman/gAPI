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

public static class InvokeResponseDoneDtoMultipartFormDataContentSerializer
{

    [IsMultipartFormDataContentSerializer]
    public static void Write(this MultipartFormDataContent ___content, string ___name, InvokeResponseDoneDto value)
    {
        RequestIdMultipartFormDataContentSerializer.Write(___content, "RequestId", value.RequestId);
        ServiceIdMultipartFormDataContentSerializer.Write(___content, "ServiceId", value.ServiceId);
        ServiceMethodIdMultipartFormDataContentSerializer.Write(___content, "MethodId", value.MethodId);
    }
}