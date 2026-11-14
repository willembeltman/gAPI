using System.Buffers.Text;
using System.Security.Cryptography;

namespace gAPI.Core.Ids;

public record FabricManagerId(string Value)
{
    public static FabricManagerId New()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        var token = Base64Url.EncodeToString(bytes);
        return new FabricManagerId(token);
    }

    public override string ToString()
    {
        return Value;
    }
}
