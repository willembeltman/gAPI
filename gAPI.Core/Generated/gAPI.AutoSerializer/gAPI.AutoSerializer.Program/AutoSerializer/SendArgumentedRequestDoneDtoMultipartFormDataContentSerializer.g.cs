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

public static class SendArgumentedRequestDoneDtoMultipartFormDataContentSerializer
{

    [IsMultipartFormDataContentSerializer]
    public static void Write(this MultipartFormDataContent ___content, string ___name, SendArgumentedRequestDoneDto value)
    {
        RequestIdMultipartFormDataContentSerializer.Write(___content, "RequestId", value.RequestId);
    }
}