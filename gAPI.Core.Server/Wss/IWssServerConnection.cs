using gAPI.Core.Dtos;
using gAPI.Core.Ids;
using gAPI.Core.Server.Wss;

namespace gAPI.Core.Interfaces;

// Warning: This interface is only for escaping the gAPI core module to the application specific implementation
// Do not use this interface for mocking as it doesn't expose the full interface
public interface IWssServerConnection : IAsyncDisposable
{
    Task<SendRequestDoneDto> Send_SendRequest_ToClientAsync(SendRequestDto message, CancellationToken ct);
    Task Send_SendRequestCancelled_ToClientAsync(SendRequestCancelledDto message, CancellationToken ct);
    IAsyncEnumerable<InvokeResponseDto> Send_InvokeRequest_ToClientAsync(InvokeRequestDto request, CancellationToken ct);
    Task Send_InvokeCancelled_ToClientAsync(InvokeRequestCancelledDto request, CancellationToken ct);
    Task Send_StreamingRequest_ToClientAsync(StreamingRequestDto request, CancellationToken ct);
    Task Send_StreamingResponse_ToClientAsync(StreamingResponseDto response, CancellationToken ct);
    bool HasRequest(RequestId requestId);
}