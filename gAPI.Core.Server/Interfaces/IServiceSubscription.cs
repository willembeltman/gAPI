using gAPI.Core.Dtos;
using gAPI.Core.Ids;

namespace gAPI.Core.Server.Interfaces;

public interface IServiceSubscription
{
    ClientConnectionId ClientConnectionId { get; }
    ServiceSubscriptionId ServiceSubscriptionId { get; }
    ServiceId ServiceId { get; }
    SessionId SessionId { get; }
    UserId UserId { get; }

    Task Send_StreamingRequest_ToClientAsync(StreamingRequestDto request, CancellationToken ct);
    Task Send_StreamingResponse_ToClientAsync(StreamingResponseDto response, CancellationToken ct);

    Task Send_FabricSendRequest_ToClientAsync(SendRequestDto sendRequest, CancellationToken ct);
    Task Send_FabricInvokeRequest_ToClientAsync(InvokeRequestDto invokeRequest, CancellationToken ct);

    Task SendRequestAsync(SendRequestDto request, CancellationToken ct);
    IAsyncEnumerable<byte[]> InvokeRequestAsync(InvokeRequestDto request, CancellationToken ct);

    Task Send_FabricStreamingRequest_ToClientAsync(StreamingRequestDto request, CancellationToken ct);
    Task Send_FabricStreamingResponse_ToClientAsync(StreamingResponseDto response, CancellationToken ct);
    Task Send_FabricInvokeRequestCancelled_ToClientAsync(InvokeRequestCancelledDto cancel, CancellationToken ct);
    Task Send_FabricSendRequestCancelled_ToClientAsync(SendRequestCancelledDto cancel, CancellationToken ct);
}