using System.Net;
using System.Net.Sockets;

namespace gAPI.Fabric.Models;

public sealed class Server(int port) : IAsyncDisposable
{
    private readonly TcpListener Listener = new TcpListener(IPAddress.Any, port);
    private readonly CancellationTokenSource Cts = new();
    private readonly State State = new();

    public async Task StartAsync()
    {
        Listener.Start();

        while (!Cts.IsCancellationRequested)
        {
            var tcpClient = await Listener.AcceptTcpClientAsync(Cts.Token);
            var connection = new Connection(State, tcpClient);
            _ = connection.RunAsync();
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