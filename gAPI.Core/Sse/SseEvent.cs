using gAPI.Dtos;
using System.Text.Json;

namespace gAPI.Sse;

public class SseEvent
{
    public SseEvent(SendRequestDto sseMessage)
    {
        EventName = "SendRequestDto";
        EventData = JsonSerializer.Serialize(sseMessage);
    }

    public string EventName { get; }
    public string? EventData { get; }
}