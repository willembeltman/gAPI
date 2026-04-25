using gAPI.FabricNode.Helpers;
using System.Net;
using System.Net.Sockets;

namespace gAPI.FabricNode;

public sealed class FabricServer(int port, IConsole Console) : IAsyncDisposable
{
    private readonly TcpListener Listener = new(IPAddress.Any, port);
    private readonly CancellationTokenSource ListenerCts = new();
    public readonly FabricManager Manager = new(Console);

    public async Task StartAsync()
    {
        Listener.Start();

        StartupHelper.ShowFabricNodeStarted(port);

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