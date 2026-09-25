using gAPI.Fabric.Server.Helpers;
using gAPI.Fabric.Server.Interfaces;
using gAPI.Fabric.Server.Monitoring;
using System.Net;
using System.Net.Sockets;

namespace gAPI.Fabric.Server.Services;

public sealed class FabricServer : IAsyncDisposable
{
    private readonly TcpListener Listener;
    private readonly CancellationTokenSource ListenerCts = new();
    private readonly IFabricStatusSink StatusSink;

    public int Port { get; }
    public FabricManager Manager { get; }

    public FabricServer(int port, IFabricStatusSink? statusSink = null)
    {
        Port = port;
        StatusSink = statusSink ?? NullFabricStatusSink.Instance;
        Listener = new TcpListener(IPAddress.Any, port);
        Manager = new FabricManager(StatusSink);
    }

    [Obsolete("Use FabricServer(int, IFabricStatusSink?) instead.")]
    public FabricServer(int port, IConsole console)
        : this(port, new ConsoleStatusSinkAdapter(console))
    {
    }

    public async Task StartAsync()
    {
        Listener.Start();

        StatusSink.WriteInfo($"FABRIC Port: {Port}");

        try
        {
            while (!ListenerCts.IsCancellationRequested)
            {
                var tcpClient = await Listener.AcceptTcpClientAsync(ListenerCts.Token);
                Manager.StartNewFabricHost(tcpClient);
            }
        }
        catch (OperationCanceledException) when (ListenerCts.IsCancellationRequested)
        {
        }
    }

    public async Task DisconnectAllAsync()
    {
        await Manager.DisconnectAllAsync();
    }

    public Task StopAsync() => ListenerCts.CancelAsync();

    public FabricDashboardSnapshot GetDashboardSnapshot()
        => Manager.GetDashboardSnapshot(Port);

    public async ValueTask DisposeAsync()
    {
        Listener.Stop();
        Listener.Dispose();
        await ListenerCts.CancelAsync();
        ListenerCts.Dispose();
        await Manager.DisposeAsync();
    }
}
