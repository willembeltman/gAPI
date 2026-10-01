using gAPI.Core.AttributesSerializers;
using System;
using System.Collections.Generic;
using System.Text;

namespace gAPI.Core.Serializers;

public static class ByteArraySerializer
{
    [IsMultipartFormDataContentSerializer]
    public static void Write(this MultipartFormDataContent content, string name, byte[] value)
    {
        string prefix = string.IsNullOrEmpty(name) ? "" : $"{name}.";

        if (value != null)
        {
            var base64String = Convert.ToBase64String(value);
            content.Add(new StringContent(base64String), $"{prefix}BinaryData");
        }
    }
}
