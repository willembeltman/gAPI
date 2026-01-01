using System.Threading.Tasks;

namespace gAPI.Sse
{
    public interface ISseManagerBase
    {
        Task MessageReceived(SseMessage message);
    }
}