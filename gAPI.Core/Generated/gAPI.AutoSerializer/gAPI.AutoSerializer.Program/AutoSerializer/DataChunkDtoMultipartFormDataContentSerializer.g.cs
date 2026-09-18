using gAPI.Core.Attributes;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Dtos;
using gAPI.Core.Serializers;
using System;
using System.Buffers.Binary;
using System.Text;

#nullable enable
namespace gAPI.Core.Dtos;

public static class DataChunkDtoMultipartFormDataContentSerializer
{

    [IsMultipartFormDataContentSerializer]
    public static void Write(this MultipartFormDataContent ___content, string ___name, DataChunkDto value)
    {
        ___content.Add(new ByteArrayContent(value.Data), "Data", "file");
        ___content.Add(new StringContent(value.Offset.ToString()), "Offset");
    }
}