using System.Net;
using System.Net.Sockets;

namespace gAPI.Fabric;

public sealed class BusServer(int port) : IAsyncDisposable
{
    private readonly TcpListener Listener = new TcpListener(IPAddress.Any, port);
    private readonly CancellationTokenSource Cts = new();

    public ConnectionRegistry Connections { get; } = new();
    public ServiceRegistry Services { get; } = new();

    public async Task StartAsync()
    {
        Listener.Start();

        while (!Cts.IsCancellationRequested)
        {
            var tcpClient = await Listener.AcceptTcpClientAsync(Cts.Token);
            var busClient = new BusClient(this, tcpClient);
            _ = busClient.RunAsync();
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
