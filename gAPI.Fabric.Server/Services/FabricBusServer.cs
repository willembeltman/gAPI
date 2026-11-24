using gAPI.Core.Server.Dashboard;
using gAPI.Fabric.Server.Config;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;

namespace gAPI.Fabric.Server.Services;

public sealed class FabricBusServer : IHostedService, IAsyncDisposable
{
    private readonly TcpListener Listener;
    private readonly FabricManager Manager;

    public ILogger<FabricBusServer> Logger { get; }

    private readonly FabricConfig Config;

    private CancellationTokenSource? ListenerCts;


    public FabricBusServer(
        ILoggerFactory loggerFactory,
        FabricConfig config,
        FabricManager manager)
    {
        Config = config;
        Manager = manager;
        Logger = loggerFactory.CreateLogger<FabricBusServer>();

        Listener = new TcpListener(IPAddress.Any, Config.BusPort);
    }


    public async Task StartAsync(CancellationToken ct2)
    {
        ListenerCts = CancellationTokenSource.CreateLinkedTokenSource(ct2);
        var ct = ListenerCts.Token;

        _ = Task.Run(async () =>
        {
            Listener.Start();

            Logger.LogInformation("Fabric started on port: {Config.BusPort}", Config.BusPort);

            try
            {
                while (!ListenerCts.IsCancellationRequested)
                {
                    var tcpClient = await Listener.AcceptTcpClientAsync(ct);
                    Manager.StartNewBusFabricHost(tcpClient);
                }
            }
            catch (OperationCanceledException) when (ListenerCts.IsCancellationRequested)
            {
            }
            finally
            {
                ListenerCts.Dispose();
                ListenerCts = null;
            }
        }, ct);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (ListenerCts == null)
            return;
        await ListenerCts.CancelAsync();
    }

    public async ValueTask DisposeAsync()
    {
        Listener.Stop();
        Listener.Dispose();

        if (ListenerCts != null)
        {
            await ListenerCts.CancelAsync();
            ListenerCts.Dispose();
        }
    }
}
