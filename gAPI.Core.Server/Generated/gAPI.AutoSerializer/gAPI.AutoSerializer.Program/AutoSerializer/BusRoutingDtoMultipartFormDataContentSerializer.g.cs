using gAPI.Core.Attributes;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Ids;
using gAPI.Core.Serializers;
using gAPI.Core.Server.Dtos;
using System;
using System.Buffers.Binary;
using System.Text;

#nullable enable
namespace gAPI.Core.Server.Dtos;

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
        TimeSpanMultipartFormDataContentSerializer.Write(___content, $"{___prefix}Timeout", value.Timeout);
        ___content.Add(new StringContent(((int)value.deliveryMode).ToString()), $"{___prefix}deliveryMode");
        ___content.Add(new StringContent(((int)value.To).ToString()), $"{___prefix}To");
        if (value.ToBusConnectionId != null)
            BusConnectionIdMultipartFormDataContentSerializer.Write(___content, $"{___prefix}ToBusConnectionId", value.ToBusConnectionId);
        ___content.Add(new StringContent(value.ThrowIfNoReceiver.ToString()), $"{___prefix}ThrowIfNoReceiver");
    }
}