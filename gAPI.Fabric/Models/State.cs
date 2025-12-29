using gAPI.Fabric.Collections;
using gAPI.Fabric.Helpers;
using gAPI.Fabric.Types;

namespace gAPI.Fabric.Models;

public class State
{
    private ConnectionCollection Connections = new();
    private ServiceCollection Services = new();

    public ConnectionId AddConnection(Connection connection)
    {
        return Connections.AddConnection(connection);
    }
    public void RemoveConnection(Connection connection)
    {
        Connections.RemoveConnection(connection.Id);
    }

    public void Subscribe(ServiceId serviceId, UserId userId, ScopeId scopeId, Connection connection)
    {
        var subscriberId = new SubscriptionId(userId, scopeId, connection.Id);
        var service = Services.GetOrCreate(serviceId);
        service.Subscribe(subscriberId, connection);
    }
    public void UnSubscribe(ServiceId serviceId, UserId userId, ScopeId scopeId, Connection connection)
    {
        var subscriberId = new SubscriptionId(userId, scopeId, connection.Id);
        var service = Services.TryGet(serviceId);
        if (service == null)
            return;
        service.UnSubscribe(subscriberId, connection);
        if (service.Users.Count == 0 && service.Scopes.Count == 0)
            Services.Remove(serviceId);
    }
    
    public void PublishToAll(ServiceId serviceId, byte[] messageData)
    {
        var service = Services.TryGet(serviceId);
        if (service == null) return;
        service.Publish(serviceId, messageData);
    }
    public void PublishToUser(ServiceId serviceId, UserId userId, byte[] messageData)
    {
        var service = Services.TryGet(serviceId);
        if (service == null) return;
        var user = service.Users.TryGet(userId);
        if (user == null) return;
        user.Publish(serviceId, messageData);
    }
    public void PublishToScope(ServiceId serviceId, ScopeId scopeId, byte[] messageData)
    {
        var service = Services.TryGet(serviceId);
        if (service == null) return;
        var scope = service.Scopes.TryGet(scopeId);
        if (scope == null) return;
        scope.Publish(serviceId, messageData);
    }
}