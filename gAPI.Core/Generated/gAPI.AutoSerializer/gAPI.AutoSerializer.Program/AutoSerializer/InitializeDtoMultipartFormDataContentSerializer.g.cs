using gAPI.Core.Attributes;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Dtos;
using gAPI.Core.Serializers;
using System;
using System.Buffers.Binary;
using System.Text;

#nullable enable
namespace gAPI.Core.Dtos;

public static class InitializeDtoMultipartFormDataContentSerializer
{

    [IsMultipartFormDataContentSerializer]
    public static void Write(this MultipartFormDataContent ___content, string ___name, InitializeDto value)
    {
        ___content.Add(new StringContent(value.SessionId), "SessionId");
        if (value.StateData != null)
            ___content.Add(new StringContent(value.StateData), "StateData");
    }
}