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

public static class InvokeArgumentRequestDtoMultipartFormDataContentSerializer
{

    [IsMultipartFormDataContentSerializer]
    public static void Write(this MultipartFormDataContent ___content, string ___name, InvokeArgumentRequestDto value)
    {
        RequestIdMultipartFormDataContentSerializer.Write(___content, "RequestId", value.RequestId);
        ___content.Add(new StringContent(value.ArgumentIndex.ToString()), "ArgumentIndex");
    }
}