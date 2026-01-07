using gAPI.Sse;
using gAPI.Ids;
using System.Collections.Concurrent;

namespace gAPI.FabricNode.Models;

public record User(UserId id)
{
    public UserId Id { get; } = id;
    public ConcurrentDictionary<FabricHostId, FabricHost> Connections { get; } = new();
    
    public void Subscribe(FabricHost connection)
    {
        Connections[connection.Id] = connection;
    }
    public void Unsubscribe(FabricHost connection)
    {
        Connections.TryRemove(connection.Id, out _);
    }
    public void Publish(Service service, SseServiceMethodId sseServiceMethodId, string messageData)
    {
        foreach (var connection in Connections.Values)
        {
            connection.SendMessageToClient(
                new SseMessage(service.Id, sseServiceMethodId, Id, null, messageData));
        }
    }
}
