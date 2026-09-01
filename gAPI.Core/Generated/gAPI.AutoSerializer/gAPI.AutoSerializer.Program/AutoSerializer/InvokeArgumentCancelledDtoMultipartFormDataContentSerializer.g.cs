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

public static class InvokeArgumentCancelledDtoMultipartFormDataContentSerializer
{

    [IsMultipartFormDataContentSerializer]
    public static void Write(this MultipartFormDataContent ___content, string ___name, InvokeArgumentCancelledDto value)
    {
        RequestIdMultipartFormDataContentSerializer.Write(___content, "RequestId", value.RequestId);
        ___content.Add(new StringContent(value.ArgumentIndex.ToString()), "ArgumentIndex");
        GuidSerializer.Write(___content, "StreamId", value.StreamId);
        if (value.Reason != null)
            ___content.Add(new StringContent(value.Reason), "Reason");
    }
}