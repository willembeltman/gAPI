using gAPI.Core.Dtos;
using gAPI.Core.Ids;
using gAPI.Core.Server.Wss;

namespace gAPI.Core.Interfaces;

// Warning: This interface is only for escaping the gAPI core module to the application specific implementation
// Do not use this interface for mocking as it doesn't expose the full interface
public interface ISignalRInvoker : IAsyncDisposable
{
    IAsyncEnumerable<InvokeResponseDto> Send_InvokeRequest_ToClientAsync(WssServiceSubscription hubHost, InvokeRequestDto request, CancellationToken ct);
    Task Send_SendRequest_ToClientAsync(WssServiceSubscription hubHost, SendRequestDto message, CancellationToken ct);
    Task Send_SendArgumentedRequest_ToClientAsync(WssServiceSubscription hubHost, SendRequestDto message, CancellationToken ct);
    Task Send_InvokeArgumentRequest_ToClientAsync(WssServiceSubscription hubHost, InvokeArgumentRequestDto request, CancellationToken ct);
    Task Send_InvokeArgumentResponse_ToClientAsync(WssServiceSubscription hubHost, InvokeArgumentResponseDto response, CancellationToken ct);
    bool HasRequest(RequestId requestId);
}