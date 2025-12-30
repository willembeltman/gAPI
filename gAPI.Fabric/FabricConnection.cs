using gAPI.Fabric.Models;
using gAPI.Fabric.Types;
using System.Net.Sockets;

namespace gAPI.Fabric;

public sealed class FabricConnection : IAsyncDisposable
{
    private readonly ConnectionManager Manager;
    private readonly TcpClient TcpClient;
    private readonly NetworkStream Stream;
    private readonly AutoResetQueue<SseMessage> SendQueue;
    private readonly FabricConnectionReceiver Receiver;
    private readonly FabricConnectionSender Sender;

    public FabricConnectionId Id { get; }

    public FabricConnection(ConnectionManager manager, TcpClient tcpClient)
    {
        Id = manager.AddConnection(this);

        Manager = manager;
        TcpClient = tcpClient;
        Stream = tcpClient.GetStream();
        SendQueue = new AutoResetQueue<SseMessage>();
        Receiver = new FabricConnectionReceiver(this, Stream, Manager);
        Sender = new FabricConnectionSender(this, Stream, SendQueue);
    }

    public async Task RunAsync(CancellationToken ct)
    {
        await Task.WhenAll(
            Receiver.ReceiveLoop(ct),
            Sender.SendLoop(ct));
        await DisposeAsync();
    }

    public void SendMessage(SseMessage message)
    {
        SendQueue.Enqueue(message);
    }

    public async ValueTask DisposeAsync()
    {
        Manager.RemoveConnection(this);

        await Stream.DisposeAsync();
        TcpClient.Dispose();
        SendQueue.Dispose();
    }
}