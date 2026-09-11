using gAPI.Core.Dtos;
using System.Text.Json;

namespace gAPI.Core.Sse;

public class SseEvent
{
    public SseEvent(SendRequestDto request)
    {
        EventName = "SendRequestDto";
        EventData = JsonSerializer.Serialize(request);
    }
    public SseEvent(SendRequestCancelledDto cancel)
    {
        EventName = "SendRequestCancelledDto";
        EventData = JsonSerializer.Serialize(cancel);
    }

    public string EventName { get; }
    public string? EventData { get; }
}