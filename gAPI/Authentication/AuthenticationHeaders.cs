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
        string ipAdress,
        string? cookieData,
        StringValues sessionData,
        StringValues stateData)
    {
        Path = path;
        Query = query;
        IpAdress = ipAdress;
        CookieData = cookieData;
        SessionData = sessionData;
        StateData = stateData;
        CookieExpires = DateTimeOffset.UtcNow.AddDays(7);
    }

    public PathString Path { get; }
    public QueryString Query { get; }
    public string IpAdress { get; }
    public bool UpdateCookie { get; private set; }
    public string? CookieData { get; private set; }
    public DateTimeOffset? CookieExpires { get; private set; }

    public StringValues SessionData { get; set; }
    public StringValues StateData { get; set; }

    public string SessionId
    {
        get
        {
            var sessionIdString = "SessionId=";
            for (int i = 0; i < SessionData.Count; i++)
            {
                var a = SessionData[i] ?? "";
                if (a.StartsWith(sessionIdString))
                {
                    return a.Substring(sessionIdString.Length);
                }
            }
            return string.Empty;
        }
        set
        {
            var strings = SessionData.ToList();
            var sessionIdString = "SessionId=";
            var sessionIdValue = $"{sessionIdString}{value}";
            for (int i = 0; i < strings.Count; i++)
            {
                var a = SessionData[i] ?? "";
                if (a.StartsWith(sessionIdString))
                {
                    strings[i] = sessionIdValue;
                    return;
                }
            }
            strings.Add(sessionIdValue);
        }
    }


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

