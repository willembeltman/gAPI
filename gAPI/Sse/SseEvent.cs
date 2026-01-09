namespace gAPI.Sse;

public class SseEvent
{
    public SseEvent(
        string messageType,
        SseMessage? sseMessage = null)
    {
        EventName = messageType;
        SseMessage = sseMessage;
    }

    public string EventName { get; }
    public SseMessage? SseMessage { get; }
}