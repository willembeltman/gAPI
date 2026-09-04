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

public static class StreamingResponseDtoMultipartFormDataContentSerializer
{

    [IsMultipartFormDataContentSerializer]
    public static void Write(this MultipartFormDataContent ___content, string ___name, StreamingResponseDto value)
    {
        SessionIdMultipartFormDataContentSerializer.Write(___content, "ResponseFromSessionId", value.ResponseFromSessionId);
        RoutingDtoMultipartFormDataContentSerializer.Write(___content, "Routing", value.Routing);
        ___content.Add(new StringContent(value.ArgumentIndex.ToString()), "ArgumentIndex");
        StreamIdMultipartFormDataContentSerializer.Write(___content, "StreamId", value.StreamId);
        ___content.Add(new StringContent(value.IsCompleted.ToString()), "IsCompleted");
        ___content.Add(new StringContent(value.StateIsChanged.ToString()), "StateIsChanged");
        if (value.StateData != null)
            ___content.Add(new StringContent(value.StateData), "StateData");
        ___content.Add(new ByteArrayContent(value.BinaryData), "BinaryData", "file");
    }
}