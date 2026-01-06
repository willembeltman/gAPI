using gAPI.FabricNode.Collections;
using gAPI.Types;
using System.Net.Sockets;

namespace gAPI.FabricNode
{
    public class FabricManager
    {
        private readonly FabricHostCollection Connections = new();
        private readonly ServiceCollection Services = new();

        public Task StartFabricHost(TcpClient tcpClient)
        {
            var fabricHost = new FabricHost(
                this,
                tcpClient, 
                Connections);
            _ = Task.Run(fabricHost.RunAsync);
            return Task.CompletedTask;
        }

        public void Subscribe(FabricHost connection,ServiceId serviceId, UserId userId, SessionId sessionId)
        {
            Console.WriteLine(
                $"ConnectionManager.Subscribe " +
                $"serviceId {serviceId}, " +
                $"userId {userId}, " +
                $"sessionId {sessionId}, " +
                $"connectionId {connection.Id}"); 
            Services[serviceId]
                .Subscribe(connection, userId, sessionId);
        }
        public void Unsubscribe(FabricHost connection, ServiceId serviceId, UserId userId, SessionId sessionId)
        {
            Console.WriteLine(
                $"ConnectionManager.Unsubscribe " +
                $"serviceId {serviceId}, " +
                $"userId {userId}, " +
                $"sessionId {sessionId}, " +
                $"connectionId {connection.Id}");
            Services[serviceId]
                .Unsubscribe(connection, userId, sessionId);
        }
        public void Publish(FabricHost connection, ServiceId serviceId, UserId? userId, SessionId? sessionId, string messageData)
        {
            Console.WriteLine(
                $"ConnectionManager.Publish " +
                $"serviceId {serviceId}, " +
                $"userId {userId}," +
                $"sessionId {sessionId}," +
                $"connectionId {connection.Id}");
            Services[serviceId]
                .Publish(userId,  sessionId, messageData);
        }

        public async Task DisconnectAllAsync()
        {
            await DisposeAsync();
        }
        public async Task DisposeAsync()
        {
            foreach (var conn in Connections.All)
                await conn.DisposeAsync();
        }
}
}