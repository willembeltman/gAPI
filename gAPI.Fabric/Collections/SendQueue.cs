using gAPI.Fabric.Models;
using gAPI.Fabric.Types;
using System.Collections;
using System.Collections.Concurrent;

namespace gAPI.Fabric.Collections;

public class SendQueue : IEnumerable<Message>, IDisposable
{
    private readonly ConcurrentQueue<Message> Queue = new();
    private readonly AutoResetEvent SendSignal = new(false);
    private bool KillSwitch = false;

    public void Enqueue(ServiceId serviceId, UserId? userId, ScopeId? scopeId, byte[] messageData)
    {
        Queue.Enqueue(new Message(serviceId, userId, scopeId, messageData));
        SendSignal.Set();
    }

    public IEnumerator<Message> GetEnumerator()
    {
        return GetEnumerable().GetEnumerator();
    }

    public IEnumerable<Message> GetEnumerable()
    {
        while (!KillSwitch)
            if (SendSignal.WaitOne(100))
                while (Queue.TryDequeue(out var message))
                    yield return message;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Dispose()
    {
        KillSwitch = true;
        SendSignal.Set();
        SendSignal.Dispose();
    }
}