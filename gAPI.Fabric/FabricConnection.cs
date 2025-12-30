using gAPI.Fabric.Models;
using gAPI.Fabric.Types;
using System.Net.Sockets;

namespace gAPI.Fabric;

public sealed class FabricConnection : IAsyncDisposable
{
    private readonly ConnectionManager Manager;
    private readonly TcpClient TcpClient;
    private readonly CancellationToken Ct;
    private readonly NetworkStream Stream;
    private readonly AutoResetQueue<SseMessage> SendQueue;
    public FabricConnectionReceiver Receiver { get; }
    public FabricConnectionSender Sender { get; }

    public FabricConnectionId Id { get; }

    public FabricConnection(ConnectionManager manager, TcpClient tcpClient, CancellationToken ct)
    {
        Id = manager.AddConnection(this);

        Manager = manager;
        TcpClient = tcpClient;
        Ct = ct;
        Stream = tcpClient.GetStream();
        SendQueue = new AutoResetQueue<SseMessage>();
        Receiver = new FabricConnectionReceiver(this, Stream, Manager, Ct);
        Sender = new FabricConnectionSender(this, Stream, SendQueue, Ct);
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