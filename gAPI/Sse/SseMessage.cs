using gAPI.Ids;

namespace gAPI.Sse;

public class SseMessage
{
    public SseMessage(
        SseServiceId serviceId,
        SseServiceMethodId serviceMethodId,
        UserId? userId,
        SessionId? sessionId,
        string data)
    {
        ServiceId = serviceId;
        ServiceMethodId = serviceMethodId;
        UserId = userId;
        SessionId = sessionId;
        Data = data;
    }

    public SseServiceId ServiceId { get; }
    public SseServiceMethodId ServiceMethodId { get; }
    public UserId? UserId { get; }
    public SessionId? SessionId { get; }
    public string Data { get; }
}