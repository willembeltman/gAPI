using gAPI.Fabric.Types;
using System.Net.Sockets;

namespace gAPI.Fabric;

public class FabricConnectionSender(
    FabricConnection Connection,
    NetworkStream Stream, 
    AutoResetQueue<SseMessage> SendQueue,
    CancellationToken ct)
{
    public async Task SendLoop()
    {
        var Writer = new BinaryWriter(Stream);
        Writer.Write(Connection.Id.Value); 
        foreach (var message in SendQueue.GetEnumerable(ct))
        {
            Writer.Write(message.ServiceId.Value);
            Writer.Write(message.Data.Length);
            Writer.Write(message.Data);
            if (ct.IsCancellationRequested) break;
        }
    }

    public void SendMessage(SseMessage message)
    {
        SendQueue.Enqueue(message);
    }
}