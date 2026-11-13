using gAPI.Core.Dtos;
using gAPI.Core.Helpers;
using gAPI.Core.Ids;
using gAPI.Core.Server.Dtos;
using gAPI.Core.Server.Helpers;
using System.Collections.Concurrent;

namespace gAPI.Core.Server.Collections;

public class StreamingCache
{
    public readonly ConcurrentDictionary<RequestId, CancellationTokenSourceDispose> Timeouts = [];

    public readonly ConcurrentDictionary<ServiceSubscriptionId, TaskCompletionSource<SubscribeAckDto>> SubscribeTcss = [];
    public readonly ConcurrentDictionary<ServiceSubscriptionId, TaskCompletionSource<UnsubscribeAckDto>> UnsubscribeTcss = [];

    public readonly ConcurrentDictionary<ServiceSubscriptionId, TaskCompletionSource<BusSubscribeAckDto>> BusSubscribeTcss = [];
    public readonly ConcurrentDictionary<ServiceSubscriptionId, TaskCompletionSource<BusUnsubscribeAckDto>> BusUnsubscribeTcss = [];

    public readonly ConcurrentDictionary<RequestId, TaskCompletionSource<SendRequestDoneDto>> PendingClientSendRequests = [];
    public readonly ConcurrentDictionary<RequestId, TaskCompletionSource<InvokeRequestAckDto>> PendingClientInvokeRequests = [];

    public readonly ConcurrentDictionary<RequestId, TaskCompletionSource<SendRequestDoneDto>> PendingFabricSendRequests = [];
    public readonly ConcurrentDictionary<RequestId, TaskCompletionSource<InvokeRequestAckDto>> PendingFabricInvokeRequests = [];

    public readonly ConcurrentDictionary<BusRoutingDto, TaskCompletionSource<BusSendRequestDoneDto>> PendingBusSendRequests = [];
    public readonly ConcurrentDictionary<BusRoutingDto, TaskCompletionSource<BusInvokeRequestAckDto>> PendingBusInvokeRequests = [];

    public readonly ConcurrentDictionary<RequestArgumentIndexDto, IAsyncEnumerableRegistration> StreamingRequestHandlers = [];
    public readonly ConcurrentDictionary<RequestArgumentIndexStreamDto, Action<StreamingResponseDto>> StreamingResponseHandlers = [];

    public readonly ConcurrentDictionary<RequestArgumentIndexDto, IBusAsyncEnumerableRegistration> BusStreamingRequestHandlers = [];
    public readonly ConcurrentDictionary<RequestArgumentIndexStreamDto, Action<BusStreamingResponseDto>> BusStreamingResponseHandlers = [];


    public readonly ConcurrentDictionary<SessionId, TaskCompletionSource<string?>> PendingGetSessionRequests = [];
}
