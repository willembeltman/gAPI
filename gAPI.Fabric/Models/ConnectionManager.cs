using gAPI.Fabric;
using gAPI.FabricClient.Collections;
using gAPI.Types;

namespace gAPI.FabricClient.Models
{
    public class ConnectionManager
    {
        private readonly FabricHostCollection Connections = new();
        private readonly ServiceCollection Services = new();

        public FabricHostId AddConnection(FabricHost connection)
        {
            return Connections.AddConnection(connection);
        }
        public void RemoveConnection(FabricHost connection)
        {
            Connections.RemoveConnection(connection.Id);
        }

        public void Subscribe(ServiceId serviceId, UserId userId, SessionId sessionId, FabricHost connection)
        {
            var subscriberId = new SubscriptionId(userId, sessionId, connection.Id);
            var service = Services.GetOrCreate(serviceId);
            service.Subscribe(subscriberId, connection);
        }
        public void UnSubscribe(ServiceId serviceId, UserId userId, SessionId sessionId, FabricHost connection)
        {
            var subscriberId = new SubscriptionId(userId, sessionId, connection.Id);
            var service = Services.TryGet(serviceId);
            if (service == null)
                return;
            service.UnSubscribe(subscriberId, connection);
            if (service.Users.Count == 0 && service.Scopes.Count == 0)
                Services.Remove(serviceId);
        }

        public void Publish(ServiceId serviceId, UserId? userId, SessionId? sessionId, string messageData)
        {
            //Console.WriteLine($"{DateTime.Now:HH:mm:ss.FFF}: ConnectionManager.Publish");
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

        internal async Task StopAllAsync()
        {
            foreach (var conn in Connections.All)
                await conn.DisposeAsync();
        }
    }
}