using System.Buffers.Text;
using System.Security.Cryptography;

namespace gAPI.Core.Ids;

public record PlaneConnectionId(string Value)
{
    public static PlaneConnectionId New()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        var token = Base64Url.EncodeToString(bytes);
        return new PlaneConnectionId(token);
    }

    public override string ToString()
    {
        return Value;
    }
}