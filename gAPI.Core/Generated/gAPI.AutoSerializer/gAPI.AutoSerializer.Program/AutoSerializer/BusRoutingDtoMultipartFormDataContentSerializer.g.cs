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

public static class BusRoutingDtoMultipartFormDataContentSerializer
{

    [IsMultipartFormDataContentSerializer]
    public static void Write(this MultipartFormDataContent ___content, string ___name, BusRoutingDto value)
    {
        string ___prefix = string.IsNullOrEmpty(___name) ? "" : $"{___name}.";
        
        BusConnectionIdMultipartFormDataContentSerializer.Write(___content, $"{___prefix}FromBusConnectionId", value.FromBusConnectionId);
        RequestIdMultipartFormDataContentSerializer.Write(___content, $"{___prefix}RequestId", value.RequestId);
        ServiceIdMultipartFormDataContentSerializer.Write(___content, $"{___prefix}ServiceId", value.ServiceId);
        ServiceMethodIdMultipartFormDataContentSerializer.Write(___content, $"{___prefix}MethodId", value.MethodId);
        TimeSpanMultipartFormDataContentSerializer.Write(___content, $"{___prefix}TimeOut", value.TimeOut);
        ___content.Add(new StringContent(((int)value.deliveryMode).ToString()), $"{___prefix}deliveryMode");
        ___content.Add(new StringContent(((int)value.To).ToString()), $"{___prefix}To");
        if (value.ToBusConnectionId != null)
            BusConnectionIdMultipartFormDataContentSerializer.Write(___content, $"{___prefix}ToBusConnectionId", value.ToBusConnectionId);
    }
}