using gAPI.Sse;

namespace gAPI.Interfaces;

public interface ISseManagerBase : IAsyncDisposable
{
    Task MessageReceived(SseMessage message);
}