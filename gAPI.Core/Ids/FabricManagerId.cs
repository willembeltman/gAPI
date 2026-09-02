using Microsoft.AspNetCore.WebUtilities;
using System.Security.Cryptography;

namespace gAPI.Core.Ids;

public record FabricManagerId(string Value)
{
    public static FabricManagerId New()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        var token = WebEncoders.Base64UrlEncode(bytes);
        return new FabricManagerId(token);
    }

    public override string ToString()
    {
        return Value;
    }
}
