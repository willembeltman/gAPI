using System;
using System.Threading.Tasks;

namespace gAPI.Sse
{
    public interface ISseManagerBase : IAsyncDisposable
    {
        Task MessageReceived(SseMessage message);
    }
}