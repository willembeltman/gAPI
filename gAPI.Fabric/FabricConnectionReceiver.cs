using gAPI.Fabric.Models;
using gAPI.Fabric.Types;
using System.Net.Sockets;

namespace gAPI.Fabric;

public class FabricConnectionReceiver(FabricConnection connection, NetworkStream stream, ConnectionManager manager)
{
    public Task ReceiveLoop(CancellationToken ct)
    {
        var r = new BinaryReader(stream);
        while (!ct.IsCancellationRequested)
        {
            switch (ReadMessageType(r))
            {
                case ReceivedMessageType.Subscribe:
                    manager.Subscribe(ReadServiceId(r), ReadUserId(r), ReadSessionId(r), connection);
                    break;
                case ReceivedMessageType.UnSubscribe:
                    manager.UnSubscribe(ReadServiceId(r), ReadUserId(r), ReadSessionId(r), connection);
                    break;
                case ReceivedMessageType.PublishToAll:
                    manager.PublishToAll(ReadServiceId(r), ReadMessageData(r));
                    break;
                case ReceivedMessageType.PublishToUser:
                    manager.PublishToUser(ReadServiceId(r), ReadUserId(r), ReadMessageData(r));
                    break;
                case ReceivedMessageType.PublishToScope:
                    manager.PublishToScope(ReadServiceId(r), ReadSessionId(r), ReadMessageData(r));
                    break;
            }
        }
        return Task.CompletedTask;
    }

    private ReceivedMessageType ReadMessageType(BinaryReader Reader)
    {
        return (ReceivedMessageType)Reader.ReadByte();
    }
    private ServiceId ReadServiceId(BinaryReader Reader)
    {
        var serviceName = Reader.ReadString();
        var serviceId = new ServiceId(serviceName);
        return serviceId;
    }
    private UserId ReadUserId(BinaryReader Reader)
    {
        var userIdNull = Reader.ReadBoolean();
        var userIdValue = userIdNull ? null : Reader.ReadString();
        var userId = new UserId(userIdValue);
        return userId;
    }
    private SessionId ReadSessionId(BinaryReader Reader)
    {
        var sessionIdValue = Reader.ReadString();
        var sessionId = new SessionId(sessionIdValue);
        return sessionId;
    }
    private string ReadMessageData(BinaryReader Reader)
    {
        var messageData = Reader.ReadString();
        var messageEnd = Reader.ReadString();
        var messageValid = messageEnd == "[EndOfData]";
        if (!messageValid) throw new Exception("This looks like a buffer overflow hack. Please go away.");
        return messageData;
    }
}