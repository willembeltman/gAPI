using gAPI.Core.Dtos;
using System.Text.Json;

namespace gAPI.Core.Sse;

public class SseEvent
{
    public SseEvent(string eventName, object eventData)
    {
        EventName = eventName;
        EventData = JsonSerializer.Serialize(eventData);
    }
    //public SseEvent(SendRequestClientDto request)
    //{
    //    EventName = "SendRequestClientDto";
    //    EventData = JsonSerializer.Serialize(request);
    //}
    //public SseEvent(SendRequestCancelledClientDto cancel)
    //{
    //    EventName = "SendRequestCancelledClientDto";
    //    EventData = JsonSerializer.Serialize(cancel);
    //}
    //public SseEvent(InvokeRequestClientDto request)
    //{
    //    EventName = "InvokeRequestClientDto";
    //    EventData = JsonSerializer.Serialize(request);
    //}
    //public SseEvent(InvokeRequestCancelledClientDto request)
    //{
    //    EventName = "InvokeRequestCancelledClientDto";
    //    EventData = JsonSerializer.Serialize(request);
    //}
    //public SseEvent(StreamingRequestClientDto request)
    //{
    //    EventName = "StreamingRequestClientDto";
    //    EventData = JsonSerializer.Serialize(request);
    //}
    //public SseEvent(StreamingResponseClientDto request)
    //{
    //    EventName = "StreamingResponseClientDto";
    //    EventData = JsonSerializer.Serialize(request);
    //}

    public string EventName { get; }
    public string? EventData { get; }
}