using gAPI.Core.Dtos;
using gAPI.Core.Helpers;
using gAPI.Core.Ids;
using System.Collections.Concurrent;

namespace gAPI.Core.Server.Collections;

public class StreamingCache
{
    public readonly ConcurrentDictionary<RoutingDto, ResettableTimeout> Timeouts = [];
    public readonly ConcurrentDictionary<RoutingDto, TaskCompletionSource<SendRequestDoneDto>> PendingSendRequests = [];
    public readonly ConcurrentDictionary<RoutingDto, TaskCompletionSource<InvokeRequestDoneDto>> PendingInvokeRequests = [];
    public readonly ConcurrentDictionary<(RoutingDto RequestId, int ArgumentIndex), Func<StreamId, CancellationToken, Task>> StreamingRequestHandlers = [];
    public readonly ConcurrentDictionary<(RoutingDto RequestId, int ArgumentIndex, StreamId StreamId), StreamingResponseDto> PendingStreamingResponses = [];
    public readonly ConcurrentDictionary<(RoutingDto RequestId, int ArgumentIndex, StreamId StreamId), Action<StreamingResponseDto>> StreamingResponseHandlers = [];
    public readonly ConcurrentDictionary<SessionId, TaskCompletionSource<string?>> PendingGetSessionRequests = [];

    //public readonly ConcurrentDictionary<RoutingDto, TaskCompletionSource<SendRequestDoneDto>> PendingSendRequests = [];
    //public readonly ConcurrentDictionary<RoutingDto, TaskCompletionSource<InvokeRequestDoneDto>> PendingInvokeRequests = [];
    //public readonly ConcurrentDictionary<(RoutingDto RequestId, int ArgumentIndex, StreamId StreamId), Action<StreamingResponseDto>> StreamingResponseHandlers = [];
    public readonly ConcurrentDictionary<RoutingDto, byte> ArgumentRoutes = [];

}
