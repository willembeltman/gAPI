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

public static class UpdateSessionDtoMultipartFormDataContentSerializer
{

    [IsMultipartFormDataContentSerializer]
    public static void Write(this MultipartFormDataContent ___content, string ___name, UpdateSessionDto value)
    {
        SessionIdMultipartFormDataContentSerializer.Write(___content, "SessionId", value.SessionId);
        if (value.CookieData != null)
            ___content.Add(new StringContent(value.CookieData), "CookieData");
    }
}