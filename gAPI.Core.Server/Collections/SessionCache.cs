using gAPI.Core.Ids;
using System.Collections.Concurrent;

namespace gAPI.Core.Server.Collections;

public sealed class SessionCache
{
    private readonly TimeSpan Expiration = TimeSpan.FromMinutes(30);

    private readonly ConcurrentDictionary<SessionId, CachedSession> Sessions = new();

    public void AddOrUpdate(SessionId sessionId, string? cookieData)
    {
        Sessions[sessionId] = new CachedSession(cookieData, DateTime.UtcNow + Expiration);
    }
    public bool TryGet(SessionId sessionId, out string? cookieData)
    {
        cookieData = null;
        if (Sessions.TryGetValue(sessionId, out var cached))
        {
            if (cached.ExpiresAt < DateTime.UtcNow)
            {
                // expired
                Sessions.TryRemove(sessionId, out _);
                return false;
            }

            cookieData = cached.CookieData;
            Sessions[sessionId] = new CachedSession(cookieData, DateTime.UtcNow + Expiration);
            return true;
        }
        return false;
    }
    public void Remove(SessionId sessionId)
    {
        Sessions.TryRemove(sessionId, out _);
    }

    public void Cleanup()
    {
        var now = DateTime.UtcNow;
        foreach (var kvp in Sessions)
        {
            if (kvp.Value.ExpiresAt < now)
            {
                Sessions.TryRemove(kvp.Key, out _);
            }
        }
    }

    private record CachedSession(string? CookieData, DateTime ExpiresAt);
}
