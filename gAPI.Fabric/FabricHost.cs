using gAPI.Fabric.Models;
using gAPI.Fabric.Types;
using System.IO;
using System.Net.Sockets;

namespace gAPI.Fabric;

public sealed class FabricHost : IAsyncDisposable
{
    private readonly ConnectionManager Manager;
    private readonly TcpClient TcpClient;
    private readonly CancellationToken Ct;
    private readonly NetworkStream Stream;
    private readonly AutoResetQueue<SseMessage> SendQueue;
    public FabricHostId Id { get; }

    public FabricHost(ConnectionManager manager, TcpClient tcpClient, CancellationToken ct)
    {
        Id = manager.AddConnection(this);

        Manager = manager;
        TcpClient = tcpClient;
        Ct = ct;
        Stream = tcpClient.GetStream();
        SendQueue = new AutoResetQueue<SseMessage>();
    }

    public async Task RunAsync()
    {
        _ = Task.Run(ReceiveLoop);
        _ = Task.Run(SendLoop);
        //await DisposeAsync();
    }
    public async Task ReceiveLoop()
    {
        var fc = new FabricConverter();
        var r = new BinaryReader(Stream);
        while (!Ct.IsCancellationRequested)
        {
            switch (fc.ReadMessageType(r))
            {
                case ReceivedMessageType.Subscribe:
                    Manager.Subscribe(fc.ReadServiceId(r), fc.ReadUserId(r), fc.ReadSessionId(r), this);
                    break;
                case ReceivedMessageType.UnSubscribe:
                    Manager.UnSubscribe(fc.ReadServiceId(r), fc.ReadUserId(r), fc.ReadSessionId(r), this);
                    break;
                case ReceivedMessageType.Publish:
                    Manager.Publish(fc.ReadServiceId(r), fc.ReadNullableUserId(r), fc.ReadNullableSessionId(r), fc.ReadMessageData(r));
                    break;
            }
        }
    }
    public async Task SendLoop()
    {
        var fc = new FabricConverter();
        var w = new BinaryWriter(Stream);
        fc.WriteFabricHostId(w, Id);
        foreach (var message in SendQueue.GetEnumerable(Ct))
        {
            fc.WriteServiceId(w, message.ServiceId);
            fc.WriteNullableUserId(w, message.UserId);
            fc.WriteNullableSessionId(w, message.SessionId);
            fc.WriteMessageData(w, message.Data);

            if (Ct.IsCancellationRequested) break;
        }
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