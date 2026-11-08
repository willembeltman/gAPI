using gAPI.Core.Server.Dtos;

namespace gAPI.Core.Server.Interfaces;

public interface IBusConnection : IBaseBusConnection
{
    Task Receive_BusFireAndForget_FromFabricAsync(BusSendRequestDto requestDto, CancellationToken ct);
    Task Receive_BusSendRequest_FromFabricAsync(BusSendRequestDto requestDto, CancellationToken ct);
    IAsyncEnumerable<byte[]> Receive_BusInvokeRequest_FromFabricAsync(BusInvokeRequestDto requestDto, CancellationToken ct);
}
