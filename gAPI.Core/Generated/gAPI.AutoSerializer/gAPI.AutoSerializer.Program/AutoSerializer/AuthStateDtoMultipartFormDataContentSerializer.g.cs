using gAPI.Core.Attributes;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Dtos;
using gAPI.Core.Serializers;
using System;
using System.Buffers.Binary;
using System.Text;

#nullable enable
namespace gAPI.Core.Dtos;

public static class AuthStateDtoMultipartFormDataContentSerializer
{

    [IsMultipartFormDataContentSerializer]
    public static void Write(this MultipartFormDataContent ___content, string ___name, AuthStateDto value)
    {
        if (value.User != null)
            AuthStateUserDtoMultipartFormDataContentSerializer.Write(___content, "User", value.User);
        ___content.Add(new StringContent(value.ForceReconnect.ToString()), "ForceReconnect");
    }
}