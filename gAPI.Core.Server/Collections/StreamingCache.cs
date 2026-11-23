using gAPI.Core.Dtos;
using gAPI.Core.Helpers;
using gAPI.Core.Ids;
using gAPI.Core.Server.Dtos;
using gAPI.Core.Server.Helpers;
using System.Collections.Concurrent;

namespace gAPI.Core.Server.Collections;

public class StreamingCache
{
    public readonly ConcurrentDictionary<RequestId, CancellationTokenSourceDispose> WssReceivedTimeouts = [];
    public readonly ConcurrentDictionary<RequestId, CancellationTokenSourceDispose> FabricSenderTimeouts = [];
    public readonly ConcurrentDictionary<RequestId, CancellationTokenSourceDispose> BusReceivedTimeouts = [];
    public readonly ConcurrentDictionary<RequestId, CancellationTokenSourceDispose> BusSenderTimeouts = [];
    public readonly ConcurrentDictionary<RequestId, CancellationTokenSourceDispose> PlaneReceivedTimeouts = [];
    public readonly ConcurrentDictionary<RequestId, CancellationTokenSourceDispose> PlaneSenderTimeouts = [];

    public readonly ConcurrentDictionary<ServiceId, ConcurrentDictionary<object, ServiceSubscriptionId>> BusServiceSubscriptionIds = [];
    //public readonly ConcurrentDictionary<ServiceId, ConcurrentDictionary<object, ServiceSubscriptionId>> PlaneServiceSubscriptionIds = [];

    public readonly ConcurrentDictionary<SessionId, TaskCompletionSource<string?>> PendingGetSessionRequests = [];

    public readonly ConcurrentDictionary<ServiceSubscriptionId, TaskCompletionSource<SubscribeAckDto>> SubscribeTcss = [];
    public readonly ConcurrentDictionary<ServiceSubscriptionId, TaskCompletionSource<UnsubscribeAckDto>> UnsubscribeTcss = [];

    public readonly ConcurrentDictionary<ServiceSubscriptionId, TaskCompletionSource<BusSubscribeAckDto>> BusSubscribeTcss = [];
    public readonly ConcurrentDictionary<ServiceSubscriptionId, TaskCompletionSource<BusUnsubscribeAckDto>> BusUnsubscribeTcss = [];

    public readonly ConcurrentDictionary<ServiceSubscriptionId, TaskCompletionSource<PlaneSubscribeAckDto>> PlaneSubscribeTcss = [];
    public readonly ConcurrentDictionary<ServiceSubscriptionId, TaskCompletionSource<PlaneUnsubscribeAckDto>> PlaneUnsubscribeTcss = [];

    public readonly ConcurrentDictionary<RequestId, TaskCompletionSource<SendRequestDoneDto>> PendingClientSendRequests = [];
    public readonly ConcurrentDictionary<RequestId, TaskCompletionSource<InvokeRequestAckDto>> PendingClientInvokeRequestAcks = [];
    public readonly ConcurrentDictionary<RequestId, TaskCompletionSource<InvokeRequestDoneDto>> PendingClientInvokeRequestDones = [];

    public readonly ConcurrentDictionary<RequestId, TaskCompletionSource<SendRequestDoneDto>> PendingFabricSendRequestDones = [];
    public readonly ConcurrentDictionary<RequestId, TaskCompletionSource<InvokeRequestAckDto>> PendingFabricInvokeRequestAcks = [];
    public readonly ConcurrentDictionary<RequestId, TaskCompletionSource<InvokeRequestDoneDto>> PendingFabricInvokeRequestDones = [];

    public readonly ConcurrentDictionary<BusRoutingDto, TaskCompletionSource<BusSendRequestDoneDto>> PendingBusSendRequestDones = [];
    public readonly ConcurrentDictionary<BusRoutingDto, TaskCompletionSource<BusInvokeRequestAckDto>> PendingBusInvokeRequestAcks = [];
    public readonly ConcurrentDictionary<BusRoutingDto, TaskCompletionSource<BusInvokeRequestDoneDto>> PendingBusInvokeRequestDones = [];

    public readonly ConcurrentDictionary<PlaneRoutingDto, TaskCompletionSource<PlaneSendRequestDoneDto>> PendingPlaneSendRequestDones = [];
    public readonly ConcurrentDictionary<PlaneRoutingDto, TaskCompletionSource<PlaneInvokeRequestAckDto>> PendingPlaneInvokeRequestAcks = [];
    public readonly ConcurrentDictionary<PlaneRoutingDto, TaskCompletionSource<PlaneInvokeRequestDoneDto>> PendingPlaneInvokeRequestDones = [];

    public readonly ConcurrentDictionary<RequestArgumentIndexDto, IAsyncEnumerableRegistration> StreamingRequestHandlers = [];
    public readonly ConcurrentDictionary<RequestArgumentIndexStreamDto, Action<StreamingResponseDto>> StreamingResponseHandlers = [];

    public readonly ConcurrentDictionary<RequestArgumentIndexDto, IBusAsyncEnumerableRegistration> BusStreamingRequestHandlers = [];
    public readonly ConcurrentDictionary<RequestArgumentIndexStreamDto, Action<BusStreamingResponseDto>> BusStreamingResponseHandlers = [];
    
    public readonly ConcurrentDictionary<RequestArgumentIndexDto, IPlaneAsyncEnumerableRegistration> PlaneStreamingRequestHandlers = [];
    public readonly ConcurrentDictionary<RequestArgumentIndexStreamDto, Action<PlaneStreamingResponseDto>> PlaneStreamingResponseHandlers = [];

}
