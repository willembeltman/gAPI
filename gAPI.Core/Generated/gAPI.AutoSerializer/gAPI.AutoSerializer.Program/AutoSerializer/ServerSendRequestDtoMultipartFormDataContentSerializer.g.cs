using gAPI.Core.Attributes;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Dtos;
using gAPI.Core.Serializers;
using System;
using System.Buffers.Binary;
using System.Text;

#nullable enable
namespace gAPI.Core.Dtos;

public static class ServerSendRequestDtoMultipartFormDataContentSerializer
{

    [IsMultipartFormDataContentSerializer]
    public static void Write(this MultipartFormDataContent ___content, string ___name, ServerSendRequestDto value)
    {
        string ___prefix = string.IsNullOrEmpty(___name) ? "" : $"{___name}.";
        
        ServerRoutingDtoMultipartFormDataContentSerializer.Write(___content, $"{___prefix}Routing", value.Routing);
        ___content.Add(new StringContent(value.Path), $"{___prefix}Path");
        if (value.QueryString != null)
            ___content.Add(new StringContent(value.QueryString), $"{___prefix}QueryString");
        ___content.Add(new ByteArrayContent(value.IpAddress), $"{___prefix}IpAddress", "file");
        if (value.CookieData != null)
            ___content.Add(new StringContent(value.CookieData), $"{___prefix}CookieData");
        if (value.StateData != null)
            ___content.Add(new StringContent(value.StateData), $"{___prefix}StateData");
        if (value.SessionData != null)
            ___content.Add(new StringContent(value.SessionData), $"{___prefix}SessionData");
        ___content.Add(new ByteArrayContent(value.BinaryData), $"{___prefix}BinaryData", "file");
    }
}