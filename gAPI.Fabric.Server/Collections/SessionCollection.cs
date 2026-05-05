using gAPI.Fabric.Models;
using gAPI.Core.Ids;
using System.Collections;
using System.Collections.Concurrent;

namespace gAPI.Fabric.Collections;

public class SessionCollection : IEnumerable<Session>
{
    private readonly ConcurrentDictionary<SessionId, Session> Sessions = new();

    public Session? TryGet(SessionId sessionId)
    {
        if (!Sessions.TryGetValue(sessionId, out var scope))
            return null;
        return scope;
    }
    public void TryAdd(SessionId sessionId, FabricHost connection)
    {
        var session = Sessions.GetOrAdd(sessionId, sessionId => new Session(sessionId));
        session.Subscribe(connection);
    }
    public void TryRemove(SessionId sessionId, FabricHost connection)
    {
        if (!Sessions.TryGetValue(sessionId, out var session)) return;
        session.Unsubscribe(connection);
        if (session.Connections.Count == 0)
            Sessions.TryRemove(sessionId, out _);
    }

    public IEnumerator<Session> GetEnumerator()
    {
        return Sessions.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}