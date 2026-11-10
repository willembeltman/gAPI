using gAPI.Core.Ids;
using gAPI.Core.Server.Dtos;

namespace gAPI.Core.Server.Interfaces;

public interface IFabricBaseBusConnection : IBaseBusConnection
{
    Task OnDisconnectedAsync(CancellationToken lifeCycleCt);
    Task OnRestoreSubscriptionsAsync(CancellationToken lifeCycleCt);

    Task Send_FireAndForgetRequest_ToServiceAsync(BusSendRequestDto requestDto, CancellationToken ct);
    Task Send_SendRequest_ToServiceAsync(BusSendRequestDto requestDto, CancellationToken ct);
    IAsyncEnumerable<byte[]> Send_InvokeRequest_ToServiceAsync(BusInvokeRequestDto requestDto, CancellationToken ct);
}
