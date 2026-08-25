using gAPI.Core.Extensions;
using gAPI.Core.Ids;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Net;

namespace gAPI.Core.Server.Authentication;

public class AuthenticationHeaders
{
    public PathString Path { get; }
    public QueryString Query { get; }
    public IPAddress IpAdress { get; }
    public SessionId SessionId { get; set; }
    public string? StateData { get; set; }
    public string? CookieData { get; private set; }
    public string? CookieHash { get; private set; }
    public bool UpdateCookie { get; private set; }
    public DateTimeOffset? CookieExpires { get; private set; } = DateTimeOffset.UtcNow.AddDays(7);

    public string EncodedPath
        => WebUtility.UrlEncode(Path) ?? throw new Exception("Please initialize first");

    public AuthenticationHeaders(
        PathString path,
        QueryString query,
        IPAddress ipAdress,
        string? cookieData,
        string? stateData,
        SessionId sessionId)
    {
        Path = path;
        Query = query;
        IpAdress = ipAdress;
        SessionId = sessionId;
        StateData = stateData;
        CookieData = cookieData;
        CookieHash = CookieData != null ? StringHelper.HashString(CookieData) : null;
    }

    public string CreateNewCookie()
    {
        CookieData = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        CookieExpires = DateTimeOffset.UtcNow.AddDays(7);
        CookieHash = StringHelper.HashString(CookieData);
        UpdateCookie = true;
        return CookieHash;
    }

    public void RemoveCookie()
    {
        CookieData = null;
        CookieExpires = null;
        UpdateCookie = true;
    }
}

