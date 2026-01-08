using gAPI.FabricNode.Models;
using gAPI.Ids;
using System.Collections.Concurrent;

namespace gAPI.FabricNode.Collections;

public class SessionCollection 
{
    private readonly ConcurrentDictionary<SessionId, Session> Sessions = new();

    public Session this[SessionId sessionId]
    {
        get => Sessions.GetOrAdd(
            sessionId,
            sessionId => new Session(sessionId));
        set => Sessions[sessionId] = value;
    }

    public Session? TryGet(SessionId sessionId)
    {
        if (!Sessions.TryGetValue(sessionId, out var scope))
            return null;
        return scope;
    }
    internal void TryAdd(SessionId sessionId, FabricHost connection)
    {
        var session = Sessions.GetOrAdd(sessionId, sessionId => new Session(sessionId));
        session.Subscribe(connection);
    }
    internal void TryRemove(SessionId sessionId, FabricHost connection)
    {
        if (!Sessions.TryGetValue(sessionId, out var session)) return;
        session.Unsubscribe(connection);
        if (session.Connections.Count == 0)
            Sessions.TryRemove(sessionId, out _);
    }
}