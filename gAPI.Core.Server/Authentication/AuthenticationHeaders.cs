using gAPI.Extensions;
using gAPI.Ids;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.IO;
using System.Net;

namespace gAPI.Authentication;

public class AuthenticationHeaders
{

    public PathString Path { get; }
    public QueryString Query { get; }
    public IPAddress IpAdress { get; }
    public SessionId SessionId { get; set; }
    public StringValues SessionData => new StringValues(SessionId.Value);
    public string? CookieData { get; private set; }
    public bool UpdateCookie { get; private set; }
    public DateTimeOffset? CookieExpires { get; private set; } = DateTimeOffset.UtcNow.AddDays(7);

    public string? CookieHash
        => CookieData != null ? StringHelper.HashString(CookieData) : null;
    public string EncodedPath
        => WebUtility.UrlEncode(Path) ?? throw new Exception("Please initialize first");

    public AuthenticationHeaders(
        PathString path,
        QueryString query,
        IPAddress ipAdress,
        string? cookieData,
        SessionId sessionId)
    {
        Path = path;
        Query = query;
        IpAdress = ipAdress;
        SessionId = sessionId;
        CookieData = cookieData;
    }

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

