using Microsoft.AspNetCore.WebUtilities;
using System.Security.Cryptography;

namespace gAPI.Ids;

public readonly record struct RequestId(string Value)
{
    public static RequestId New()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        var token = WebEncoders.Base64UrlEncode(bytes);
        return new RequestId(token);
    }

    public override string ToString()
    {
        return Value;
    }
}