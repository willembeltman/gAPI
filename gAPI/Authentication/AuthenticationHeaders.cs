using gAPI.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Net;

namespace gAPI.Authentication;

public class AuthenticationHeaders
{
    public AuthenticationHeaders(
        PathString path,
        QueryString query,
        IPAddress ipAdress,
        string? cookieData,
        string sessionId)
    {
        Path = path;
        Query = query;
        IpAdress = ipAdress;
        CookieData = cookieData;
        SessionId = sessionId;
        CookieExpires = DateTimeOffset.UtcNow.AddDays(7);
    }

    public PathString Path { get; }
    public QueryString Query { get; }
    public IPAddress IpAdress { get; }
    public bool UpdateCookie { get; private set; }
    public string? CookieData { get; private set; }
    public DateTimeOffset? CookieExpires { get; private set; }
    public string SessionId { get; set; }

    public string? CookieHash
        => CookieData != null ? StringHelper.HashString(CookieData) : null;
    public string EncodedPath
        => WebUtility.UrlEncode(Path) ?? throw new Exception("Please initialize first");

    public string CreateNewCookie()
    {
        CookieData = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        CookieExpires = DateTimeOffset.UtcNow.AddDays(7);
        UpdateCookie = true;
        return StringHelper.HashString(CookieData);
    }

    public void RemoveCookie()
    {
        CookieData = null;
        CookieExpires = null;
        UpdateCookie = true;
    }
}

