using gAPI.Core.Attributes;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Dtos;
using gAPI.Core.Serializers;
using System;
using System.Buffers.Binary;
using System.Text;

#nullable enable
namespace gAPI.Core.Dtos;

public static class AuthStateUserDtoMultipartFormDataContentSerializer
{

    [IsMultipartFormDataContentSerializer]
    public static void Write(this MultipartFormDataContent ___content, string ___name, AuthStateUserDto value)
    {
        string ___prefix = string.IsNullOrEmpty(___name) ? "" : $"{___name}.";
        
        GuidSerializer.Write(___content, $"{___prefix}Id", value.Id);
        ___content.Add(new StringContent(value.UserName), $"{___prefix}UserName");
        ___content.Add(new StringContent(value.Email), $"{___prefix}Email");
    }
}