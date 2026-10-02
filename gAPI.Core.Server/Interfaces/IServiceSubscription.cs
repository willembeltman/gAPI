using gAPI.Core.Dtos;
using gAPI.Core.Ids;

namespace gAPI.Core.Server.Interfaces;

public interface IServiceSubscription : ISpeedGetter
{
    ClientConnectionId ClientConnectionId { get; }
    ServiceSubscriptionId ServiceSubscriptionId { get; }
    ServiceId ServiceId { get; }
    SessionId SessionId { get; }
    UserId UserId { get; }

    Task Send_StreamingRequest_ToClientAsync(StreamingRequestDto message, CancellationToken ct);
    Task Send_StreamingResponse_ToClientAsync(StreamingResponseDto message, CancellationToken ct);

    Task Send_FabricSendRequest_ToClientAsync(SendRequestDto message, CancellationToken ct);
    Task Send_FabricInvokeRequest_ToClientAsync(InvokeRequestDto message, CancellationToken ct);

    Task SendRequestAsync(SendRequestDto message, CancellationToken ct);
    IAsyncEnumerable<byte[]> InvokeRequestAsync(InvokeRequestDto message, CancellationToken ct);

    Task Send_FabricStreamingRequest_ToClientAsync(StreamingRequestDto message, CancellationToken ct);
    Task Send_FabricStreamingResponse_ToClientAsync(StreamingResponseDto message, CancellationToken ct);
    Task Send_FabricInvokeRequestCancelled_ToClientAsync(InvokeRequestCancelledDto message, CancellationToken ct);
    Task Send_FabricSendRequestCancelled_ToClientAsync(SendRequestCancelledDto message, CancellationToken ct);

}