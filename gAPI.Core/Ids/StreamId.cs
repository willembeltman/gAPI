
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Cryptography;

namespace gAPI.Core.Ids;

public record StreamId(string Value)
{
    public static StreamId New()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        var token = WebEncoders.Base64UrlEncode(bytes);
        return new StreamId(token);
    }

    public override string ToString()
    {
        return Value;
    }
}