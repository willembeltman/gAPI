using gAPI.FabricClient.Models;
using gAPI.Types;
using System.Collections;
using System.Collections.Concurrent;

namespace gAPI.FabricClient.Collections;

public class SessionCollection : IEnumerable<Session>
{
    private readonly ConcurrentDictionary<SessionId, Session> Scopes = new();
    public int Count => Scopes.Count;

    public Session GetOrCreate(SessionId sessionId) => Scopes.GetOrAdd(sessionId, _ => new Session(sessionId));
    public Session? TryGet(SessionId sessionId)
    {
        if (!Scopes.TryGetValue(sessionId, out var scope))
            return null;
        return scope;
    }
    public bool Remove(SessionId sessionId) => Scopes.TryRemove(sessionId, out _);

    public IEnumerator<Session> GetEnumerator() => Scopes.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}