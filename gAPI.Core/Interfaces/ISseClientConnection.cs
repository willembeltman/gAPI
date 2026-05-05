using gAPI.Core.Dtos;

namespace gAPI.Core.Interfaces;

public interface IClientConnection : IDisposable
{
    Task MessageReceivedAsync(SendRequestDto message, CancellationToken ct);
    void SubscribeAsync(object implementation);
    void UnsubscribeAsync(object implementation);
}
