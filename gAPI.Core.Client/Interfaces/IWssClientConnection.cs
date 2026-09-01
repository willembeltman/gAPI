using gAPI.Core.Dtos;
using gAPI.Core.Ids;
using gAPI.Core.Interfaces;

namespace gAPI.Core.Client.Interfaces;

public interface IWssClientConnection : IWssLoggerFactory
{
    SessionId SessionId { get; }

    bool Initialized { get; }
    bool IsConnected { get; }

    Task Send_Subscribe_ToServerAsync(SubscribeDto subscribe, CancellationToken ct);
    Task Send_Unsubscribe_ToServerAsync(UnsubscribeDto unsubscribe, CancellationToken ct);
    Task TryConnectAsync(CancellationToken ct);
    Task ForceReconnectAsync(CancellationToken ct);

    Task Send_SendRequest_ToServerAsync(SendRequestDto sendRequest, CancellationToken ct);
    Task Send_SendRequestCancelled_ToServerAsync(SendRequestCancelledDto sendRequestCancelled, CancellationToken ct);
    Task Send_InvokeRequest_ToServerAsync(InvokeRequestDto invokeRequest, CancellationToken ct);
    Task Send_InvokeRequestCancelled_ToServerAsync(InvokeRequestCancelledDto invokeRequestCancelled, CancellationToken ct);
    Task Send_InvokeArgumentCancelled_ToServerAsync(InvokeArgumentCancelledDto invokeArgumentCancelled, CancellationToken ct);
    Task Send_InvokeResponse_ToServerAsync(InvokeResponseDto invokeResponse, CancellationToken ct);
    Task Send_InvokeResponseDone_ToServerAsync(InvokeResponseDoneDto invokeResponseDone, CancellationToken ct);

}