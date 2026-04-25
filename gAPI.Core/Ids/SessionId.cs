using gAPI.Extensions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using System.Security.Cryptography;

namespace gAPI.Ids;

public readonly record struct SessionId(string Value)
{
    public static SessionId New()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        var token = WebEncoders.Base64UrlEncode(bytes);
        return new SessionId(token);
    }

    public static bool TryParse(StringValues stringValues, out SessionId sessionId)
    {
        sessionId = new(string.Empty);
        var value = stringValues.FirstOrDefault();
        if (value == null) return false;
        sessionId = new(value);
        return true;
    }
    public static bool TryParse(IEnumerable<string> stringValues, out SessionId sessionId)
    {
        sessionId = new(string.Empty);
        var value = stringValues.FirstOrDefault();
        if (value == null) return false;
        sessionId = new(value);
        return true;
    }

    public StringValues ToStringValues()
    {
        return new StringValues(Value);
    }

    public override string ToString()
    {
        return Value;
    }
};
