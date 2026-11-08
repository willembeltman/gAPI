using System.Text.Json;

namespace gAPI.Core.Dtos;

public class SseEvent
{
    public SseEvent(string eventName, object eventData)
    {
        EventName = eventName;
        EventData = JsonSerializer.Serialize(eventData);
    }

    public string EventName { get; }
    public string? EventData { get; }
}