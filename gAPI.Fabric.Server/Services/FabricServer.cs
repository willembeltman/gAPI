using gAPI.Fabric.Server.Config;
using gAPI.Fabric.Server.Interfaces;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Net.Sockets;

namespace gAPI.Fabric.Server.Services;

public sealed class FabricServer : IHostedService, IAsyncDisposable
{
    private readonly TcpListener Listener;
    private readonly FabricManager Manager;
    private readonly FabricConfig Config;
    private readonly ConsoleBuffer Console;

    private CancellationTokenSource? ListenerCts;


    public FabricServer(
        FabricConfig config,
        FabricManager manager,
        ConsoleBuffer console)
    {
        Config = config;
        Manager = manager;
        Console = console;

        Listener = new TcpListener(IPAddress.Any, Config.Port);
    }


    public async Task StartAsync(CancellationToken ct2)
    {
        ListenerCts = CancellationTokenSource.CreateLinkedTokenSource(ct2);
        var ct = ListenerCts.Token;

        _ = Task.Run(async () =>
        {
            Listener.Start();

            Console.WriteInfo($"Fabric started on port: {Config.Port}");

            try
            {
                while (!ListenerCts.IsCancellationRequested)
                {
                    var tcpClient = await Listener.AcceptTcpClientAsync(ct);
                    Manager.StartNewFabricHost(tcpClient);
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
