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

public static class BusUnsubscribeDtoMultipartFormDataContentSerializer
{

    [IsMultipartFormDataContentSerializer]
    public static void Write(this MultipartFormDataContent ___content, string ___name, BusUnsubscribeDto value)
    {
        string ___prefix = string.IsNullOrEmpty(___name) ? "" : $"{___name}.";
        
        ServiceIdMultipartFormDataContentSerializer.Write(___content, $"{___prefix}ServiceId", value.ServiceId);
        BusConnectionIdMultipartFormDataContentSerializer.Write(___content, $"{___prefix}BusConnectionId", value.BusConnectionId);
    }
}