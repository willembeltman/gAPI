using gAPI.Sse;
using System;
using System.Threading.Tasks;

namespace gAPI.Interfaces
{
    public interface ISseManagerBase : IAsyncDisposable
    {
        Task MessageReceived(SseMessage message);
    }
}