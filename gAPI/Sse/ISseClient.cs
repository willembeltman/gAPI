using gAPI.Types;
using System;

namespace gAPI.Sse
{
    public interface ISseClient : IAsyncDisposable
    {
        ServiceId ServiceId { get; }
        SseHostId? SseHostId { get; }
    }
}