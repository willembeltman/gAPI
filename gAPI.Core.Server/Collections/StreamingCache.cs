using gAPI.Core.Dtos;
using gAPI.Core.Helpers;
using gAPI.Core.Ids;
using System.Collections.Concurrent;

namespace gAPI.Core.Server.Collections;

public class StreamingCache
{
    //public readonly ConcurrentDictionary<RequestId, ResettableTimeout> Timeouts = [];
    public readonly ConcurrentDictionary<RequestId, TaskCompletionSource<SendRequestDoneDto>> PendingSendRequests = [];
    public readonly ConcurrentDictionary<RequestId, TaskCompletionSource<InvokeRequestDoneDto>> PendingInvokeRequests = [];
    public readonly ConcurrentDictionary<(RequestId RequestId, int ArgumentIndex), Func<StreamId, CancellationToken, Task>> StreamingRequestHandlers = [];
    public readonly ConcurrentDictionary<(RequestId RequestId, int ArgumentIndex, StreamId StreamId), StreamingResponseDto> PendingStreamingResponses = [];
    public readonly ConcurrentDictionary<(RequestId RequestId, int ArgumentIndex, StreamId StreamId), Action<StreamingResponseDto>> StreamingResponseHandlers = [];
    public readonly ConcurrentDictionary<RequestId, LinkedCancellationTokenSourceWithTimeout> Timeouts = []; 
    public readonly ConcurrentDictionary<SessionId, TaskCompletionSource<string?>> PendingGetSessionRequests = [];
    //public readonly ConcurrentDictionary<RoutingDto, byte> ArgumentRoutes = []; // TODO is dit nodig?

}
