using gAPI.Fabric.Models;
using gAPI.Fabric.Types;
using System.Net.Sockets;

namespace gAPI.Fabric;

public class FabricConnectionReceiver(FabricConnection connection, NetworkStream stream, ConnectionManager manager)
{
    public Task ReceiveLoop(CancellationToken ct)
    {
        var c = new FabricConverter();
        var r = new BinaryReader(stream);
        while (!ct.IsCancellationRequested)
        {
            switch (c.ReadMessageType(r))
            {
                case ReceivedMessageType.Subscribe:
                    manager.Subscribe(c.ReadServiceId(r), c.ReadUserId(r), c.ReadSessionId(r), connection);
                    break;
                case ReceivedMessageType.UnSubscribe:
                    manager.UnSubscribe(c.ReadServiceId(r), c.ReadUserId(r), c.ReadSessionId(r), connection);
                    break;
                case ReceivedMessageType.Publish:
                    manager.Publish(c.ReadServiceId(r), c.ReadNullableUserId(r), c.ReadNullableSessionId(r), c.ReadMessageData(r));
                    break;
                //case ReceivedMessageType.PublishToAll:
                //    manager.PublishToAll(c.ReadServiceId(r), c.ReadMessageData(r));
                //    break;
                //case ReceivedMessageType.PublishToUser:
                //    manager.PublishToUser(c.ReadServiceId(r), c.ReadUserId(r), c.ReadMessageData(r));
                //    break;
                //case ReceivedMessageType.PublishToScope:
                //    manager.PublishToScope(c.ReadServiceId(r), c.ReadSessionId(r), c.ReadMessageData(r));
                //    break;
            }
        }
        return Task.CompletedTask;
    }
}
