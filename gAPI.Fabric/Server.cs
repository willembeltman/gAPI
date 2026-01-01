using gAPI.FabricClient.Models;
using System.Net;
using System.Net.Sockets;

namespace gAPI.FabricClient
{
    public sealed class Server(int port) : IAsyncDisposable
    {
        private readonly TcpListener Listener = new TcpListener(IPAddress.Any, port);
        private readonly CancellationTokenSource Cts = new();
        private readonly ConnectionManager Manager = new();

        public async Task StartAsync()
        {
            Listener.Start();

            while (!Cts.IsCancellationRequested)
            {
                var tcpClient = await Listener.AcceptTcpClientAsync(Cts.Token);
                var connection = new FabricHost(Manager, tcpClient);
                await connection.RunAsync();
            }
        }

        public async ValueTask DisposeAsync()
        {
            await Cts.CancelAsync();
            Listener.Stop();
            Listener.Dispose();
            Cts.Dispose();
        }
    }
}