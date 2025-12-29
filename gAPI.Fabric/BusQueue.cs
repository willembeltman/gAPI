using System.Collections.Concurrent;

namespace gAPI.Fabric;

public record BusMessage(
    ServiceId ServiceId, 
    byte[] MessageData);

public class BusQueue
{
    private readonly ConcurrentQueue<BusMessage> SendQueue = new();
    private readonly AutoResetEvent SendSignal = new(false);

    public void Enqueue(ServiceId serviceId, byte[] messageData)
    {
        SendQueue.Enqueue(new BusMessage(serviceId, messageData));
        SendSignal.Set();
    }

    public BusMessage? WaitForDequeue()
    {
        if (!SendSignal.WaitOne(100)) return null;
        SendQueue.TryDequeue(out var message);
        return message;
    }
}