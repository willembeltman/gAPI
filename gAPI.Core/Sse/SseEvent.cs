using gAPI.Core.Dtos;
using System.Text.Json;

namespace gAPI.Core.Sse;

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