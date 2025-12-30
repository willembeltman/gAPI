using gAPI.Fabric.Types;
using System.Net.Sockets;

namespace gAPI.Fabric;

internal class FabricConnectionSender(
    FabricConnection Connection,
    NetworkStream Stream, 
    AutoResetQueue<SseMessage> SendQueue)
{
    public Task SendLoop(CancellationToken ct)
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
        return Task.CompletedTask;
    }
}