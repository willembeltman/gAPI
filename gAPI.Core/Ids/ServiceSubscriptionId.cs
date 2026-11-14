using System.Buffers.Text;
using System.Security.Cryptography;

namespace gAPI.Core.Ids;

public record ServiceSubscriptionId(string Value)
{
    public static ServiceSubscriptionId New()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        var token = Base64Url.EncodeToString(bytes);
        return new ServiceSubscriptionId(token);
    }

    public override string ToString()
    {
        return Value;
    }
}