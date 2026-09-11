using gAPI.Core.Dtos;

namespace gAPI.Core.Server.Interfaces;

public interface IWssServerConnection : IServerConnection, IAsyncDisposable
{
    //Task Send_SendRequest_ToClientAsync(SendRequestDto message, CancellationToken ct);
    //IAsyncEnumerable<byte[]> Send_InvokeRequest_ToClientAsync(InvokeRequestDto request, CancellationToken ct);
    Task SendRequestAsync(SendRequestDto message, CancellationToken ct);
    IAsyncEnumerable<byte[]> InvokeRequestAsync(InvokeRequestDto request, CancellationToken ct);

    Task Send_StreamingRequest_ToClientAsync(StreamingRequestDto request, CancellationToken ct);
    Task Send_StreamingResponse_ToClientAsync(StreamingResponseDto response, CancellationToken ct);
    Task Send_FabricStreamingRequest_ToClientAsync(StreamingRequestDto request, CancellationToken ct);
    Task Send_FabricStreamingResponse_ToClientAsync(StreamingResponseDto response, CancellationToken ct);
    Task Send_FabricSendRequest_ToClientAsync(SendRequestDto sendRequest, CancellationToken ct);
    Task Send_FabricInvokeRequest_ToClientAsync(InvokeRequestDto invokeRequest, CancellationToken ct);
    Task Send_FabricInvokeRequestCancelled_ToClientAsync(InvokeRequestCancelledDto cancel, CancellationToken ct);
    Task Send_FabricSendRequestCancelled_ToClientAsync(SendRequestCancelledDto cancel, CancellationToken ct);
}