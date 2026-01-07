using System.Net;
using System.Net.Sockets;

namespace gAPI.FabricNode
{
    public sealed class Server(int port) : IAsyncDisposable
    {
        private readonly TcpListener Listener = new(IPAddress.Any, port);
        private readonly CancellationTokenSource ListenerCts = new();
        private readonly FabricManager Manager = new();

        public async Task StartAsync()
        {
            Listener.Start();
            while (!ListenerCts.IsCancellationRequested)
            {
                var tcpClient = await Listener.AcceptTcpClientAsync(ListenerCts.Token);
                Manager.StartNewFabricHost(tcpClient);
            }
        }

        public async Task DisconnectAllAsync()
        {
            await Manager.DisconnectAllAsync();
        }

        public async ValueTask DisposeAsync()
        {
            Listener.Stop();
            Listener.Dispose();
            await ListenerCts.CancelAsync();
            ListenerCts.Dispose();
            await Manager.DisposeAsync();
        }
    }
}