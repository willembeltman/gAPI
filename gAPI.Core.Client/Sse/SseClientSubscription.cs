using gAPI.Core.Client.Helpers;
using gAPI.Core.Client.Interfaces;
using gAPI.Core.Dtos;
using gAPI.Core.Helpers;
using gAPI.Core.Ids;
using gAPI.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;

namespace gAPI.Core.Client.Sse;

// TODO LELIJK DING GOED MAKEN, geen tijd voor.
public class SseClientSubscription : IDisposable
{
    private readonly SseClientSender Sender;
    private CancellationTokenSource Cts = new(); // Roleert elke disconnect

    public ILogger<SseClientSubscription> Logger { get; }
    public IClientAuthenticatedHttpClient HttpClient { get; }
    public ServiceId ServiceId { get; }
    public ServiceSubscriptionId ServiceSubscriptionId { get; }
    public ClientConnectionId? ClientConnectionId { get; private set; }
    public FabricConnectionId? FabricConnectionId { get; private set; }
    public FabricManagerId? FabricManagerId { get; private set; }
    public bool Initialized =>
        ServiceSubscriptionId != null &&
        ClientConnectionId != null &&
        FabricConnectionId != null &&
        FabricManagerId != null;

    private readonly ConcurrentDictionary<RequestId, CancellationTokenSourceDispose> Timeouts = [];
    private readonly ConcurrentDictionary<RequestArgumentIndexDto, IAsyncEnumerableRegistration> StreamingRequestHandlers = [];
    private readonly ConcurrentDictionary<RequestArgumentIndexStreamDto, Action<StreamingResponseDto>> StreamingResponseHandlers = [];
    private readonly ISseClientConnection SseClientConnection;

    public SseClientSubscription(
        ILoggerFactory loggerFactory,
        IClientAuthenticatedHttpClient AuthenticationService,
        ISseClientConnection sseClientConnection,
        ServiceId serviceId,
        ServiceSubscriptionId serviceSubscriptionId)
    {
        Logger = loggerFactory.CreateLogger<SseClientSubscription>();
        HttpClient = AuthenticationService;
        this.SseClientConnection = sseClientConnection;
        ServiceId = serviceId;
        ServiceSubscriptionId = serviceSubscriptionId;
        Sender = new(loggerFactory, this);
    }

    public async Task ConnectAsync()
    {
        while (!Cts.IsCancellationRequested)
        {
            await Cts.CancelAsync();
            Cts.Dispose();
            Cts = new();

            try
            {
                var url = $"/SseServiceSubscription/connect/{WebUtility.UrlEncode(ServiceId.Value)}_{WebUtility.UrlEncode(ServiceSubscriptionId.Value)}";
                using var stream = await HttpClient.GetStreamAsync(url, Cts.Token);
                using var streamReader = new StreamReader(stream);

                var buffer = new StringBuilder();
                var chunk = new char[64 * 1024];

                while (!Cts.IsCancellationRequested)
                {
                    var read = await streamReader.ReadAsync(chunk.AsMemory(0, chunk.Length), Cts.Token);
                    if (read <= 0) break;

                    buffer.Append(chunk, 0, read);

                    while (TryExtractFrame(buffer, out var frame))
                    {
                        if (!ParseFrame(frame, out string eventName, out string eventData))
                            continue;

                        switch (eventName)
                        {
                            case "ClientConnectionId":
                                if (long.TryParse(eventData, out var clientConnectionId))
                                {
                                    ClientConnectionId = new ClientConnectionId(clientConnectionId);
                                }
                                break;
                            case "FabricConnectionId":
                                if (long.TryParse(eventData, out var fabricConnectionId))
                                {
                                    FabricConnectionId = new FabricConnectionId(fabricConnectionId);
                                }
                                break;
                            case "FabricManagerId":
                                FabricManagerId = new FabricManagerId(eventData);
                                break;

                            case "SendAndForgetRequestClientDto":
                                var sendAndForgetRequest = JsonSerializer.Deserialize<SendRequestClientDto>(eventData);
                                if (sendAndForgetRequest != null)
                                    await Receive_SendAndForgetRequest_FromServerAsync(sendAndForgetRequest, Cts.Token);
                                break;
                            case "SendRequestClientDto":
                                var sendRequest = JsonSerializer.Deserialize<SendRequestClientDto>(eventData);
                                if (sendRequest != null)
                                    await Receive_SendRequest_FromServerAsync(sendRequest, Cts.Token);
                                break;
                            case "SendRequestCancelledClientDto":
                                var sendRequestCancelled = JsonSerializer.Deserialize<SendRequestCancelledClientDto>(eventData);
                                if (sendRequestCancelled != null)
                                    await Receive_SendRequestCancelled_FromServerAsync(sendRequestCancelled, Cts.Token);
                                break;

                            case "InvokeRequestClientDto":
                                var invokeRequest = JsonSerializer.Deserialize<InvokeRequestClientDto>(eventData);
                                if (invokeRequest != null)
                                    await Receive_InvokeRequest_FromServerAsync(invokeRequest, Cts.Token);
                                break;
                            case "InvokeRequestCancelledClientDto":
                                var invokeRequestCancelled = JsonSerializer.Deserialize<InvokeRequestCancelledClientDto>(eventData);
                                if (invokeRequestCancelled != null)
                                    await Receive_InvokeRequestCancelled_FromServerAsync(invokeRequestCancelled, Cts.Token);
                                break;

                            case "StreamingRequestClientDto":
                                var streamingRequest = JsonSerializer.Deserialize<StreamingRequestClientDto>(eventData);
                                if (streamingRequest != null)
                                    // Converteer naar fabric call
                                    await Receive_StreamingRequest_FromServerAsync(streamingRequest, Cts.Token);
                                break;
                            case "StreamingResponseClientDto":
                                var streamingResponse = JsonSerializer.Deserialize<StreamingResponseClientDto>(eventData);
                                if (streamingResponse != null)
                                    // Converteer naar fabric call
                                    await Receive_StreamingResponse_FromServerAsync(streamingResponse, Cts.Token);
                                break;

                            case "FabricStreamingRequestClientDto":
                                var fabricStreamingRequest = JsonSerializer.Deserialize<StreamingRequestClientDto>(eventData);
                                if (fabricStreamingRequest != null)
                                    await Receive_FabricStreamingRequest_FromServerAsync(fabricStreamingRequest, Cts.Token);
                                break;
                            case "FabricStreamingResponseClientDto":
                                var fabricStreamingResponse = JsonSerializer.Deserialize<StreamingResponseClientDto>(eventData);
                                if (fabricStreamingResponse != null)
                                    await Receive_FabricStreamingResponse_FromServerAsync(fabricStreamingResponse, Cts.Token);
                                break;

                            case "FabricDisconnectedDto":
                                var fabricDisconnectedDto = JsonSerializer.Deserialize<FabricDisconnectedDto>(eventData);
                                if (fabricDisconnectedDto != null)
                                    await Receive_FabricDisconnected_FromServerAsync(fabricDisconnectedDto, Cts.Token);
                                break;
                            case "FabricRestoreSubscriptionsDto":
                                var fabricRestoreSubscriptionsDto = JsonSerializer.Deserialize<FabricRestoreSubscriptionsDto>(eventData);
                                if (fabricRestoreSubscriptionsDto != null)
                                    await Receive_FabricRestoreSubscriptions_FromServerAsync(fabricRestoreSubscriptionsDto, Cts.Token);
                                break;
                            case "SubscribeAckDto":
                                var subscribeAckDto = JsonSerializer.Deserialize<SubscribeAckDto>(eventData);
                                if (subscribeAckDto != null)
                                    await Receive_SubscribeAck_FromServerAsync(subscribeAckDto, Cts.Token);
                                break;
                            case "UnsubscribeAckDto":
                                var unsubscribeAckDto = JsonSerializer.Deserialize<UnsubscribeAckDto>(eventData);
                                if (unsubscribeAckDto != null)
                                    await Receive_UnsubscribeAck_FromServerAsync(unsubscribeAckDto, Cts.Token);
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Error with SSE");
            }
        }
    }

    #region Helpers
    private static bool TryExtractFrame(StringBuilder buffer, out string frame)
    {
        for (int i = 0; i < buffer.Length - 1; i++)
        {
            if (buffer[i] == '\n' && buffer[i + 1] == '\n')
            {
                frame = buffer.ToString(0, i);
                buffer.Remove(0, i + 2);
                return true;
            }
        }

        frame = null!;
        return false;
    }
    private bool ParseFrame(string frame, out string eventName, out string eventData)
    {
        eventName = null!;
        eventData = null!;
        var dataBuilder = new StringBuilder();

        var lines = frame.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            if (line.StartsWith("event:"))
            {
                eventName = line[6..].TrimStart();
            }
            else if (line.StartsWith("data:"))
            {
                if (dataBuilder.Length > 0)
                    dataBuilder.Append('\n');

                dataBuilder.Append(line[5..].TrimStart());
            }
        }

        if (eventName == null)
            return false;

        eventData = dataBuilder.ToString();
        return true;
    }
    private CancellationToken CreateCancellation(RequestId requestId, CancellationToken ct)
    {
        return ct;
        //// Quick cleanup
        //var keysToRemove = new List<RequestId>();
        //foreach (var kvp in Requests)
        //{
        //    if (kvp.Value.IsCancellationRequested)
        //    {
        //        keysToRemove.Add(kvp.Key);
        //    }
        //}
        //foreach (var key in keysToRemove)
        //{
        //    Requests.Remove(key);
        //}

        //// Try get and create
        //if (Requests.TryGetValue(requestId, out var cts) == false)
        //    cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        //cts.CancelAfter(TimeSpan.FromSeconds(100));
        //return cts.Token;
    }
    #endregion

    private async Task Receive_SendAndForgetRequest_FromServerAsync(SendRequestClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Receive_SendRequest_FromServerAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        _ = Task.Run(async () =>
        {
            if (message.StateIsChanged)
                await HttpClient.UpdateStateDataAsync(
                    message.StateData,
                    ct);
            await SseClientConnection.Send_SendRequest_ToServiceAsync(
                message.Routing.ServiceId,
                message.Routing.MethodId,
                message.BinaryData,
                ct);
        }, ct);
    }

    private async Task Receive_SendRequest_FromServerAsync(SendRequestClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Receive_SendRequest_FromServerAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        ct = CreateCancellation(message.Routing.RequestId, ct);

        _ = Task.Run(async () =>
        {
            if (message.StateIsChanged)
                await HttpClient.UpdateStateDataAsync(
                    message.StateData,
                    ct);
            await SseClientConnection.Send_SendRequest_ToServiceAsync(
                message.Routing.ServiceId,
                message.Routing.MethodId,
                message.BinaryData,
                ct);

            var stateIsChanged = HttpClient.IsStateDataChanged();
            var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync() : null;
            var returnmessage = new SendRequestDoneClientDto(
                message.Routing,
                false,
                null,
                stateIsChanged,
                stateData);
            await Sender.Send_SendRequestDone_ToServerAsync(returnmessage, ct);
        }, ct);
    }
    private async Task Receive_SendRequestCancelled_FromServerAsync(SendRequestCancelledClientDto message, CancellationToken token)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Receive_SendRequestCancelled_FromServerAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        //if (Requests.TryGetValue(message.Routing.RequestId, out var cts) == false)
        //    return;

        //await cts.CancelAsync();
        //cts.Dispose();
    }

    private async Task Receive_InvokeRequest_FromServerAsync(InvokeRequestClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Receive_InvokeRequest_FromServerAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        ct = CreateCancellation(message.Routing.RequestId, ct);
        _ = Task.Run(async () =>
        {
            try
            {
                var timeoutCts = new CancellationTokenSourceDispose(message.Routing.Timeout, ct);
                Timeouts.TryAdd(message.Routing.RequestId, timeoutCts);
                ct = timeoutCts.Token;

                if (message.StateIsChanged)
                    await HttpClient.UpdateStateDataAsync(message.StateData, ct);

                var enumerable = SseClientConnection.Send_InvokeRequest_ToServiceAsync(
                    message.Routing.ServiceId,
                    message.Routing.MethodId,
                    message.BinaryData,
                    ct);

                RegisterAsyncEnumerableArgumentByte(
                    message.Routing,
                    -1,
                    enumerable,
                    timeoutCts,
                    async () =>
                    {
                        await UnRegisterAsyncEnumerableArgument(message.Routing, -1);

                        var stateIsChanged = HttpClient.IsStateDataChanged();
                        var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync() : null;
                        await Sender.Send_InvokeRequestDone_ToServerAsync(
                            new InvokeRequestDoneClientDto(
                                message.Routing,
                                false,
                                null,
                                stateIsChanged,
                                stateData
                            ), ct);
                    });

                var stateIsChanged = HttpClient.IsStateDataChanged();
                var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync() : null;
                await Sender.Send_InvokeRequestAck_ToServerAsync(
                    new InvokeRequestAckClientDto(
                        message.Routing,
                        true,
                        false,
                        null,
                        stateIsChanged,
                        stateData
                    ), ct);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Receive_InvokeRequest_FromServerAsync");
                var stateIsChanged = HttpClient.IsStateDataChanged();
                var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync() : null;
                await Sender.Send_InvokeRequestDone_ToServerAsync(
                    new InvokeRequestDoneClientDto(
                        message.Routing,
                        false,
                        new ExceptionDto(ex.Message, ex.InnerException?.Message, ex.StackTrace?.ToString()),
                        stateIsChanged,
                        stateData
                    ), ct);
            }

        }, ct);
    }
    private async Task Receive_InvokeRequestCancelled_FromServerAsync(InvokeRequestCancelledClientDto message, CancellationToken ct)
    {
        // TODO
        //if (Requests.TryGetValue(message.Routing.RequestId, out var cts) == false)
        //    return;

        //await cts.CancelAsync();
        //cts.Dispose();
    }

    private async Task Receive_StreamingRequest_FromServerAsync(StreamingRequestClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Receive_StreamingRequest_FromServerAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        ct = CreateCancellation(message.Routing.RequestId, ct);

        _ = Task.Run(async () =>
        {
            if (message.StateIsChanged)
                await HttpClient.UpdateStateDataAsync(message.StateData, ct);

            if (StreamingRequestHandlers.TryGetValue(new(message.Routing.RequestId, message.ArgumentIndex), out var handler))
            {
                var response = await handler.Invoke(message.StreamId, false, ct);

                var stateIsChanged = HttpClient.IsStateDataChanged();
                var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync() : null;
                var streamingResponseClient = new StreamingResponseClientDto(
                    response.Routing,
                    response.ArgumentIndex,
                    response.StreamId,
                    response.IsCompleted,
                    response.IsCancelled,
                    response.Exception,
                    response.BinaryData,
                    stateIsChanged,
                    stateData);
                await Sender.Send_StreamingResponse_ToServerAsync(streamingResponseClient, ct);
            }
        }, ct);
    }
    private async Task Receive_StreamingResponse_FromServerAsync(StreamingResponseClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Receive_StreamingResponse_FromServerAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        ct = CreateCancellation(message.Routing.RequestId, ct);
        _ = Task.Run(async () =>
        {
            if (message.StateIsChanged)
                await HttpClient.UpdateStateDataAsync(message.StateData, ct);

            if (StreamingResponseHandlers.TryGetValue(new(message.Routing.RequestId, message.ArgumentIndex, message.StreamId), out var responseHandler))
                responseHandler.Invoke(message);
        }, ct);
    }

    private async Task Receive_FabricStreamingRequest_FromServerAsync(StreamingRequestClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Receive_FabricStreamingRequest_FromServerAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        ct = CreateCancellation(message.Routing.RequestId, ct);

        _ = Task.Run(async () =>
        {
            if (message.StateIsChanged)
                await HttpClient.UpdateStateDataAsync(message.StateData, ct);

            if (StreamingRequestHandlers.TryGetValue(new(message.Routing.RequestId, message.ArgumentIndex), out var handler))
            {
                var response = await handler.Invoke(message.StreamId, false, ct);
                var stateIsChanged = HttpClient.IsStateDataChanged();
                var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync() : null;
                var streamingResponseClient = new StreamingResponseClientDto(
                    response.Routing,
                    response.ArgumentIndex,
                    response.StreamId,
                    response.IsCompleted,
                    response.IsCancelled,
                    response.Exception,
                    response.BinaryData,
                    stateIsChanged,
                    stateData);
                await Sender.Send_FabricStreamingResponse_ToServerAsync(streamingResponseClient, ct);
            }
        }, ct);
    }
    private async Task Receive_FabricStreamingResponse_FromServerAsync(StreamingResponseClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Receive_FabricStreamingResponse_FromServerAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        ct = CreateCancellation(message.Routing.RequestId, ct);
        _ = Task.Run(async () =>
        {
            if (message.StateIsChanged)
                await HttpClient.UpdateStateDataAsync(message.StateData, ct);

            if (StreamingResponseHandlers.TryGetValue(new(message.Routing.RequestId, message.ArgumentIndex, message.StreamId), out var responseHandler))
                responseHandler.Invoke(message);
        }, ct);
    }

    private async Task Receive_FabricDisconnected_FromServerAsync(FabricDisconnectedDto message, CancellationToken token)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Receive_FabricStreamingResponse_FromServerAsync({message})", DateTime.Now.ToString("HH:mm:ss.fff"), message);
    }
    private async Task Receive_FabricRestoreSubscriptions_FromServerAsync(FabricRestoreSubscriptionsDto message, CancellationToken token)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Receive_FabricRestoreSubscriptions_FromServerAsync({message})", DateTime.Now.ToString("HH:mm:ss.fff"), message);
    }

    private async Task Receive_UnsubscribeAck_FromServerAsync(UnsubscribeAckDto message, CancellationToken token)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Receive_UnsubscribeAck_FromServerAsync({message})", DateTime.Now.ToString("HH:mm:ss.fff"), message);
    }
    private async Task Receive_SubscribeAck_FromServerAsync(SubscribeAckDto message, CancellationToken token)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Receive_SubscribeAck_FromServerAsync({message})", DateTime.Now.ToString("HH:mm:ss.fff"), message);
    }

    protected IAsyncEnumerable<byte[]> RegisterRemoteAsyncEnumerableArgumentByte(RoutingDto routing, int argumentIndex, CancellationTokenSourceDispose timeoutCts)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(
                "RegisterRemoteAsyncEnumerableArgumentByte({serviceId}, {methodId}, {requestId}, {argumentIndex})",
                routing.ServiceId, routing.MethodId, routing.RequestId,
                argumentIndex);

        return new RemoteAsyncEnumerable<byte[]>(
            timeoutCts: timeoutCts,
            construct: (StreamId streamId, IRemoteAsyncEnumerator<byte[]> enumerator, CancellationToken ct) =>
            {
                var key = new RequestArgumentIndexStreamDto(routing.RequestId, argumentIndex, streamId);
                StreamingResponseHandlers.TryAdd(key, message =>
                {
                    if (message.IsCancelled)
                    {
                        enumerator.Complete(new TaskCanceledException(message.Exception?.Message));
                        return;
                    }

                    if (message.Exception != null)
                    {
                        enumerator.Complete(new RemoteException(message.Exception));
                        return;
                    }

                    if (message.IsCompleted)
                    {
                        enumerator.Complete();
                        return;
                    }

                    enumerator.Push(message.BinaryData);
                });
            },

            requestNext: (StreamId streamId, IRemoteAsyncEnumerator<byte[]> enumerator, CancellationToken ct) =>
            {
                return Send_StreamingRequest_ToServerAsync(
                    routing,
                    argumentIndex,
                    streamId,
                    false,
                    ct);
            },

            cancelled: (StreamId streamId, IRemoteAsyncEnumerator<byte[]> enumerator) =>
            {
                return Send_StreamingRequest_ToServerAsync(
                    routing,
                    argumentIndex,
                    streamId,
                    true,
                    CancellationToken.None);
            },

            dispose: (StreamId streamId) =>
            {
                var key = new RequestArgumentIndexStreamDto(routing.RequestId, argumentIndex, streamId);
                StreamingResponseHandlers.TryRemove(key, out _);
            });
    }
    protected IAsyncEnumerable<T> RegisterRemoteAsyncEnumerableArgument<T>(RoutingDto routing, int argumentIndex, Func<byte[], T> deserializer, CancellationTokenSourceDispose timeoutCts)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} RegisterRemoteAsyncEnumerableArgument({serviceId}, {methodId}, {requestId}, {argumentIndex})", DateTime.Now.ToString("HH:mm:ss.fff"), routing.ServiceId, routing.MethodId, routing.RequestId, argumentIndex);

        return new RemoteAsyncEnumerable<T>(
            timeoutCts: timeoutCts,
            construct: (StreamId streamId, IRemoteAsyncEnumerator<T> enumerator, CancellationToken ct) =>
            {
                var key = new RequestArgumentIndexStreamDto(routing.RequestId, argumentIndex, streamId);
                StreamingResponseHandlers.TryAdd(key, message =>
                {
                    if (message.IsCancelled)
                    {
                        enumerator.Complete(new TaskCanceledException(message.Exception?.Message));
                        return;
                    }

                    if (message.Exception != null)
                    {
                        enumerator.Complete(new RemoteException(message.Exception));
                        return;
                    }

                    if (message.IsCompleted)
                    {
                        enumerator.Complete();
                        return;
                    }

                    try
                    {
                        enumerator.Push(deserializer.Invoke(message.BinaryData));
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "RegisterRemoteAsyncEnumerableArgument");
                        enumerator.Complete(ex);
                    }
                });
            },

            requestNext: (StreamId streamId, IRemoteAsyncEnumerator<T> enumerator, CancellationToken ct) =>
            {
                return Send_FabricStreamingRequest_ToServerAsync(
                    routing,
                    argumentIndex,
                    streamId,
                    false,
                    ct);
            },

            cancelled: (StreamId streamId, IRemoteAsyncEnumerator<T> enumerator) =>
            {
                return Send_FabricStreamingRequest_ToServerAsync(
                    routing,
                    argumentIndex,
                    streamId,
                    true,
                    default);
            },

            dispose: (StreamId streamId) =>
            {
                var key = new RequestArgumentIndexStreamDto(routing.RequestId, argumentIndex, streamId);
                StreamingResponseHandlers.TryRemove(key, out _);
            });
    }

    private async Task Send_StreamingRequest_ToServerAsync(RoutingDto routing, int argumentIndex, StreamId streamId, bool cancelled, CancellationToken ct)
    {
        var stateIsChanged = HttpClient.IsStateDataChanged();
        var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync() : null;
        var streamingResponseClient = new StreamingRequestClientDto(
            routing,
            argumentIndex,
            streamId,
            cancelled,
            stateIsChanged,
            stateData);
        await Sender.Send_StreamingRequest_ToServerAsync(streamingResponseClient, ct);
    }
    private async Task Send_FabricStreamingRequest_ToServerAsync(RoutingDto routing, int argumentIndex, StreamId streamId, bool cancelled, CancellationToken ct)
    {
        var stateIsChanged = HttpClient.IsStateDataChanged();
        var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync() : null;
        var streamingResponseClient = new StreamingRequestClientDto(
            routing,
            argumentIndex,
            streamId,
            cancelled,
            stateIsChanged,
            stateData);
        await Sender.Send_FabricStreamingRequest_ToServerAsync(streamingResponseClient, ct);
    }

    public void RegisterAsyncEnumerableArgument<T>(RoutingDto routing, int argumentIndex, IAsyncEnumerable<T> source, Func<T, byte[]> serializer, CancellationTokenSourceDispose timeoutCts)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} RegisterAsyncEnumerableArgument({serviceId}, {methodId}, {requestId}, {argumentIndex})", DateTime.Now.ToString("HH:mm:ss.fff"), routing.ServiceId, routing.MethodId, routing.RequestId, argumentIndex);

        StreamingRequestHandlers.TryAdd(
            new(routing.RequestId, argumentIndex),
            new AsyncEnumerableRegistration<T>(
                async (
                    ConcurrentDictionary<StreamId, AsyncEnumerableRegistrationInstance<T>> activeStreams,
                    StreamId streamId,
                    bool cancelled,
                    CancellationToken ct) =>
                {
                    var stream = activeStreams.GetOrAdd(
                        streamId,
                        _ =>
                        {
                            return new AsyncEnumerableRegistrationInstance<T>(
                                source.GetAsyncEnumerator(timeoutCts.Token),
                                new SemaphoreSlim(1, 1),
                                timeoutCts);
                        });

                    var entered = false;
                    async Task Cleanup()
                    {
                        stream.TryRemoveFrom(activeStreams, streamId);
                        await stream.DisposeAsync(entered);
                    }

                    //if (activeStreams.TryAdd(streamId, stream) == false)
                    //{
                    //    await Cleanup();
                    //    throw new Exception("Cache");
                    //}

                    var enumerator = stream.enumerator;
                    var gate = stream.gate;

                    try
                    {
                        await gate.WaitAsync(ct);
                        entered = true;

                        var hasNext = !cancelled && !stream.IsDisposed && await enumerator.MoveNextAsync();

                        var stateIsChanged = HttpClient.IsStateDataChanged();
                        var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync() : null;

                        var response = new StreamingResponseClientDto(
                            routing,
                            argumentIndex,
                            streamId,
                            !hasNext,
                            cancelled,
                            null,
                            hasNext ? serializer(enumerator.Current) : [],
                            stateIsChanged,
                            stateData);

                        if (!hasNext)
                            await Cleanup();

                        return response;
                    }
                    catch (OperationCanceledException ex) when (
                        ct.IsCancellationRequested ||
                        timeoutCts.IsCancellationRequested)
                    {
                        Logger.LogError(ex, "RegisterAsyncEnumerableArgument Cancelled");

                        await Cleanup();

                        var stateIsChanged = HttpClient.IsStateDataChanged();
                        var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync() : null;

                        return new StreamingResponseClientDto(
                            routing,
                            argumentIndex,
                            streamId,
                            true,
                            true,
                            new ExceptionDto(ex),
                            [],
                            stateIsChanged,
                            stateData);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "RegisterAsyncEnumerableArgument exception");

                        await Cleanup();

                        var stateIsChanged = HttpClient.IsStateDataChanged();
                        var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync() : null;

                        return new StreamingResponseClientDto(
                            routing,
                            argumentIndex,
                            streamId,
                            true,
                            false,
                            new ExceptionDto(ex),
                            [],
                            stateIsChanged,
                            stateData);
                    }
                    finally
                    {
                        if (entered)
                            gate.Release();
                    }
                }));
    }
    public void RegisterAsyncEnumerableArgumentByte(RoutingDto routing, int argumentIndex, IAsyncEnumerable<byte[]> source, CancellationTokenSourceDispose timeoutCts, Func<Task> done)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} RegisterAsyncEnumerableArgumentByte({serviceId}, {methodId}, {requestId}, {argumentIndex})", DateTime.Now.ToString("HH:mm:ss.fff"), routing.ServiceId, routing.MethodId, routing.RequestId, argumentIndex);

        StreamingRequestHandlers.TryAdd(new(routing.RequestId, argumentIndex), new AsyncEnumerableRegistration<byte[]>(async (
            ConcurrentDictionary<StreamId, AsyncEnumerableRegistrationInstance<byte[]>> activeStreams,
            StreamId streamId,
            bool cancelled,
            CancellationToken ct) =>
        {
            var stream = activeStreams.GetOrAdd(
                streamId,
                _ =>
                {
                    return new AsyncEnumerableRegistrationInstance<byte[]>(
                        source.GetAsyncEnumerator(timeoutCts.Token),
                        new SemaphoreSlim(1, 1),
                        timeoutCts);
                });

            var entered = false;

            async Task Cleanup()
            {
                stream.TryRemoveFrom(activeStreams, streamId);
                await stream.DisposeAsync(entered);
                await done.Invoke();
            }

            //if (activeStreams.TryAdd(streamId, stream) == false)
            //{
            //    await Cleanup();
            //    throw new Exception("Cache");
            //}

            var enumerator = stream.enumerator;
            var gate = stream.gate;

            try
            {
                await gate.WaitAsync(ct);
                entered = true;

                var hasNext = !cancelled && !stream.IsDisposed && await enumerator.MoveNextAsync();

                var stateIsChanged = HttpClient.IsStateDataChanged();
                var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync() : null;

                var response = new StreamingResponseClientDto(
                    routing,
                    argumentIndex,
                    streamId,
                    !hasNext,
                    cancelled,
                    null,
                    hasNext ? enumerator.Current : [],
                    stateIsChanged,
                    stateData);

                if (!hasNext)
                    await Cleanup();

                return response;
            }
            catch (OperationCanceledException ex) when (
                ct.IsCancellationRequested ||
                timeoutCts.IsCancellationRequested)
            {
                Logger.LogError(ex, "RegisterAsyncEnumerableArgumentByte cancelled");

                await Cleanup();

                var stateIsChanged = HttpClient.IsStateDataChanged();
                var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync() : null;

                return new StreamingResponseClientDto(
                    routing,
                    argumentIndex,
                    streamId,
                    true,
                    true,
                    new ExceptionDto(ex),
                    [],
                    stateIsChanged,
                    stateData);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "RegisterAsyncEnumerableArgumentByte exception");

                await Cleanup();

                var stateIsChanged = HttpClient.IsStateDataChanged();
                var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync() : null;

                return new StreamingResponseClientDto(
                    routing,
                    argumentIndex,
                    streamId,
                    true,
                    false,
                    new ExceptionDto(ex),
                    [],
                    stateIsChanged,
                    stateData);
            }
            finally
            {
                if (entered)
                    gate.Release();
            }
        }));
    }
    public async Task UnRegisterAsyncEnumerableArgument(RoutingDto routing, int argumentIndex)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} UnRegisterAsyncEnumerableArgument({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), routing.ServiceId, routing.MethodId, routing.RequestId);

        if (StreamingRequestHandlers.TryRemove(new(routing.RequestId, argumentIndex), out var registration))
        {
            await registration.DisposeAsync();
        }
    }

    public void Dispose()
    {
        Cts.Cancel();
        Cts.Dispose();
        ClientConnectionId = null;
        FabricConnectionId = null;
        FabricManagerId = null;
    }
}