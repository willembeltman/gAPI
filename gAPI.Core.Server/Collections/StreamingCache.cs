using gAPI.Core.Dtos;
using gAPI.Core.Helpers;
using gAPI.Core.Ids;
using gAPI.Core.Server.Helpers;
using System.Collections.Concurrent;

namespace gAPI.Core.Server.Collections;

public class StreamingCache
{
    public readonly ConcurrentDictionary<RequestId, TaskCompletionSource<SendRequestDoneDto>> PendingClientSendRequests = [];
    public readonly ConcurrentDictionary<RequestId, TaskCompletionSource<InvokeRequestDoneDto>> PendingInvokeRequests = [];

    public readonly ConcurrentDictionary<RequestId, TaskCompletionSource<SendRequestDoneDto>> PendingFabricSendRequests = [];
    public readonly ConcurrentDictionary<RequestId, TaskCompletionSource<InvokeRequestDoneDto>> PendingFabricInvokeRequests = [];

    //public readonly ConcurrentDictionary<RequestId, TaskCompletionSource<ServerSendRequestDoneDto>> PendingServerSendRequests = [];
    //public readonly ConcurrentDictionary<RequestId, TaskCompletionSource<ServerInvokeRequestDoneDto>> PendingServerInvokeRequests = [];


    public readonly ConcurrentDictionary<RequestId, LinkedCancellationTokenSourceWithTimeout> Timeouts = [];

    public readonly ConcurrentDictionary<RequestArgumentIndexDto, IAsyncEnumerableRegistration> StreamingRequestHandlers = [];
    public readonly ConcurrentDictionary<RequestArgumentIndexStreamDto, Action<StreamingResponseDto>> StreamingResponseHandlers = [];

    public readonly ConcurrentDictionary<RequestArgumentIndexDto, IAsyncEnumerableRegistration> ServerStreamingRequestHandlers = [];
    public readonly ConcurrentDictionary<RequestArgumentIndexStreamDto, Action<ServerStreamingResponseDto>> ServerStreamingResponseHandlers = [];

    public readonly ConcurrentDictionary<SessionId, TaskCompletionSource<string?>> PendingGetSessionRequests = [];
}
