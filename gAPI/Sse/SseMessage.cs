using gAPI.Types;

namespace gAPI.Sse
{
    public class SseMessage
    {
        public SseMessage(
            ServiceId serviceId,
            UserId? userId,
            SessionId? sessionId,
            string data)
        {
            ServiceId = serviceId;
            UserId = userId;
            SessionId = sessionId;
            Data = data;
        }

        public ServiceId ServiceId { get; }
        public UserId? UserId { get; }
        public SessionId? SessionId { get; }
        public string Data { get; }
    }
}