using System.Buffers.Text;
using System.Security.Cryptography;

namespace gAPI.Core.Ids;

public record RequestId(string Value)
{
    public static RequestId New()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        var token = Base64Url.EncodeToString(bytes);
        return new RequestId(token);
    }

    public override string ToString()
    {
        return Value;
    }
}