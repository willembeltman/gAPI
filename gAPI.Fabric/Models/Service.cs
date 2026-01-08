using gAPI.FabricNode.Collections;
using gAPI.Sse;
using gAPI.Ids;
using System.Collections.Concurrent;

namespace gAPI.FabricNode.Models;

public record Service(SseServiceId id)
{
    public SseServiceId Id { get; } = id;
    public ConcurrentDictionary<FabricHostId, FabricHost> Connections { get; } = new();
    public UserCollection Users { get; } = new();
    public SessionCollection Sessions { get; } = new();

    public void Subscribe(FabricHost connection, UserId userId, SessionId sessionId)
    {
        Connections.TryAdd(connection.Id, connection);
        Users.TryAdd(userId, connection);
        Sessions.TryAdd(sessionId, connection);
    }
    public void Unsubscribe(FabricHost connection, UserId userId, SessionId sessionId)
    {
        Connections.TryRemove(connection.Id, out _);
        Users.TryRemove(userId, connection);
        Sessions.TryRemove(sessionId, connection);
    }
    public void Publish(SseServiceMethodId sseServiceMethodId, UserId? userId, SessionId? sessionId, string messageData)
    {
        if (userId != null)
        {
            var user = Users.TryGet(userId.Value);
            if (user == null) return;
            user.Publish(this, sseServiceMethodId, messageData);
        }
        else if (sessionId != null)
        {
            var scope = Sessions.TryGet(sessionId.Value);
            if (scope == null) return;
            scope.Publish(this, sseServiceMethodId, messageData);
        }
        else
        {
            //Console.WriteLine($"{DateTime.Now:HH:mm:ss.FFF}: Service.Publish");
            foreach (var fabricHost in Connections.Values)
            {
                fabricHost.SendMessageToClient(
                    new SseMessage(Id, sseServiceMethodId, null, null, messageData));
            }
        }
    }
}