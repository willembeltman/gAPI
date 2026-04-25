using gAPI.Ids;
using System.Collections.Concurrent;

namespace gAPI.Collections;

public sealed class WssSessionCache
{
    private readonly ConcurrentDictionary<SessionId, CachedSession> _sessions = new();
    private readonly TimeSpan _expiration = TimeSpan.FromMinutes(30);

    public void AddOrUpdate(SessionId sessionId, string? cookieData)
    {
        _sessions[sessionId] = new CachedSession(cookieData, DateTime.UtcNow + _expiration);
    }

    public bool TryGet(SessionId sessionId, out string? cookieData)
    {
        cookieData = null;
        if (_sessions.TryGetValue(sessionId, out var cached))
        {
            if (cached.ExpiresAt < DateTime.UtcNow)
            {
                // expired
                _sessions.TryRemove(sessionId, out _);
                return false;
            }

            cookieData = cached.CookieData;
            _sessions[sessionId] = new CachedSession(cookieData, DateTime.UtcNow + _expiration);
            return true;
        }
        return false;
    }

    public void Remove(SessionId sessionId)
    {
        _sessions.TryRemove(sessionId, out _);
    }

    public void Cleanup()
    {
        var now = DateTime.UtcNow;
        foreach (var kvp in _sessions)
        {
            if (kvp.Value.ExpiresAt < now)
            {
                _sessions.TryRemove(kvp.Key, out _);
            }
        }
    }

    private record CachedSession
    {
        public string? CookieData { get; set; }
        public DateTime ExpiresAt { get; set; }

        public CachedSession(string? cookieData, DateTime expiresAt)
        {
            CookieData = cookieData;
            ExpiresAt = expiresAt;
        }
    }
}
