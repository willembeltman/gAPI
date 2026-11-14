using System.Buffers.Text;
using System.Security.Cryptography;

namespace gAPI.Core.Ids;

public record BusConnectionId(string Value)
{
    public static BusConnectionId New()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        var token = Base64Url.EncodeToString(bytes);
        return new BusConnectionId(token);
    }

    public override string ToString()
    {
        return Value;
    }
}