using gAPI.FabricNode.Collections;
using gAPI.Ids;
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

        public void Subscribe(FabricHost connection,SseServiceId serviceId, UserId userId, SessionId sessionId)
        {
            Console.WriteLine(
                $"Subscribe " +
                $"connectionId {connection.Id}: " +
                $"{serviceId} " +
                $"(userId {userId}, " +
                $"sessionId {sessionId})");
            Services[serviceId]
                .Subscribe(connection, userId, sessionId);
        }
        public void Unsubscribe(FabricHost connection, SseServiceId serviceId, UserId userId, SessionId sessionId)
        {
            Console.WriteLine(
                $"Unsubscribe " +
                $"connectionId {connection.Id}: " +
                $"{serviceId} " +
                $"(userId {userId}, " +
                $"sessionId {sessionId})");
            Services[serviceId]
                .Unsubscribe(connection, userId, sessionId);
        }
        public void Publish(FabricHost connection, SseServiceId serviceId, SseServiceMethodId sseServiceMethodId, UserId? userId, SessionId? sessionId, string messageData)
        {
            Console.WriteLine(
                $"Publish " +
                $"connectionId {connection.Id}: " +
                $"{serviceId} " +
                $"(userId {userId}, " +
                $"sessionId {sessionId})");
            Services[serviceId]
                .Publish(sseServiceMethodId, userId,  sessionId, messageData);
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