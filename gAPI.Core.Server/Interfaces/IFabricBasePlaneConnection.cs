using gAPI.Core.Helpers;
using gAPI.Core.Ids;
using gAPI.Core.Server.Dtos;

namespace gAPI.Core.Server.Interfaces;

public interface IFabricBasePlaneConnection : IBasePlaneConnection
{
    Task OnDisconnectedAsync(CancellationToken lifeCycleCt);
    Task OnRestoreSubscriptionsAsync(CancellationToken lifeCycleCt);

    Task Send_FireAndForgetRequest_ToServiceAsync(PlaneSendRequestDto requestDto, CancellationToken ct);
    Task Send_SendRequest_ToServiceAsync(PlaneSendRequestDto requestDto, CancellationTokenSourceDispose timeoutCts);
    IAsyncEnumerable<byte[]> Send_InvokeRequest_ToServiceAsync(PlaneInvokeRequestDto requestDto, CancellationTokenSourceDispose timeoutCts);
}
