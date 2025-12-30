using gAPI.Fabric.Models;
using gAPI.Fabric.Types;
using System.Net.Sockets;

namespace gAPI.Fabric;

public sealed class FabricHost : IAsyncDisposable
{
    private readonly ConnectionManager Manager;
    private readonly TcpClient TcpClient;
    private readonly CancellationToken Ct;
    private readonly NetworkStream Stream;
    private readonly AutoResetQueue<SseMessage> SendQueue;
    public FabricHostReceiver Receiver { get; }
    public FabricHostSender Sender { get; }

    public FabricHostId Id { get; }

    public FabricHost(ConnectionManager manager, TcpClient tcpClient, CancellationToken ct)
    {
        Id = manager.AddConnection(this);

        Manager = manager;
        TcpClient = tcpClient;
        Ct = ct;
        Stream = tcpClient.GetStream();
        SendQueue = new AutoResetQueue<SseMessage>();
        Receiver = new FabricHostReceiver(this, Stream, Manager, Ct);
        Sender = new FabricHostSender(this, Stream, SendQueue, Ct);
    }

    public async Task RunAsync()
    {
        _ = Task.Run(Receiver.ReceiveLoop);
        _ = Task.Run(Sender.SendLoop);
        //await DisposeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        Manager.RemoveConnection(this);

        await Stream.DisposeAsync();
        TcpClient.Dispose();
        SendQueue.Dispose();
    }
}