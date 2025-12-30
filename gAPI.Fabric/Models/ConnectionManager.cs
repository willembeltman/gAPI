using gAPI.Fabric.Collections;
using gAPI.Fabric.Types;

namespace gAPI.Fabric.Models;

public class ConnectionManager
{
    private FabricConnectionCollection Connections = new();
    private ServiceCollection Services = new();
    
    public FabricConnectionId AddConnection(FabricConnection connection)
    {
        return Connections.AddConnection(connection);
    }
    public void RemoveConnection(FabricConnection connection)
    {
        Connections.RemoveConnection(connection.Id);
    }

    public void Subscribe(ServiceId serviceId, UserId userId, SessionId sessionId, FabricConnection connection)
    {
        var subscriberId = new SubscriptionId(userId, sessionId, connection.Id);
        var service = Services.GetOrCreate(serviceId);
        service.Subscribe(subscriberId, connection);
    }
    public void UnSubscribe(ServiceId serviceId, UserId userId, SessionId sessionId, FabricConnection connection)
    {
        var subscriberId = new SubscriptionId(userId, sessionId, connection.Id);
        var service = Services.TryGet(serviceId);
        if (service == null)
            return;
        service.UnSubscribe(subscriberId, connection);
        if (service.Users.Count == 0 && service.Scopes.Count == 0)
            Services.Remove(serviceId);
    }

    internal void Publish(ServiceId serviceId, UserId? userId, SessionId? sessionId, string messageData)
    {
        if (userId != null)
        {
            var service = Services.TryGet(serviceId);
            if (service == null) return;
            var user = service.Users.TryGet(userId.Value);
            if (user == null) return;
            user.Publish(serviceId, messageData);
        }
        else if (sessionId != null)
        {
            var service = Services.TryGet(serviceId);
            if (service == null) return;
            var scope = service.Scopes.TryGet(sessionId.Value);
            if (scope == null) return;
            scope.Publish(serviceId, messageData);
        }
        else
        {
            var service = Services.TryGet(serviceId);
            if (service == null) return;
            service.Publish(serviceId, messageData);
        }
    }

    //public void PublishToAll(ServiceId serviceId, string messageData)
    //{
    //    var service = Services.TryGet(serviceId);
    //    if (service == null) return;
    //    service.Publish(serviceId, messageData);
    //}
    //public void PublishToUser(ServiceId serviceId, UserId userId, string messageData)
    //{
    //    var service = Services.TryGet(serviceId);
    //    if (service == null) return;
    //    var user = service.Users.TryGet(userId);
    //    if (user == null) return;
    //    user.Publish(serviceId, messageData);
    //}
    //public void PublishToScope(ServiceId serviceId, SessionId sessionId, string messageData)
    //{
    //    var service = Services.TryGet(serviceId);
    //    if (service == null) return;
    //    var scope = service.Scopes.TryGet(sessionId);
    //    if (scope == null) return;
    //    scope.Publish(serviceId, messageData);
    //}
}