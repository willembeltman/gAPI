using gAPI.Core.Client.Helpers;
using gAPI.Core.Client.Interfaces;
using gAPI.Core.Dtos;
using gAPI.Core.Helpers;
using gAPI.Core.Ids;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;

namespace gAPI.Core.Client.Sse;

public class SseClient : IDisposable
{
    private readonly SseClientSender Sender;
    private CancellationTokenSource Cts = new(); // Roleert elke disconnect

    public IClientAuthenticatedHttpClient HttpClient { get; }
    public ServiceId ServiceId { get; }
    public ServiceSubscriptionId? ServiceSubscriptionId { get; private set; }
    public ClientConnectionId? ClientConnectionId { get; private set; }
    public FabricConnectionId? FabricConnectionId { get; private set; }
    public FabricManagerId? FabricManagerId { get; private set; }
    public bool Initialized =>
        ServiceSubscriptionId != null &&
        ClientConnectionId != null &&
        FabricConnectionId != null &&
        FabricManagerId != null;

    private readonly ConcurrentDictionary<RequestArgumentIndexDto, IAsyncEnumerableRegistration> StreamingRequestHandlers = [];
    private readonly ConcurrentDictionary<RequestArgumentIndexStreamDto, Action<StreamingResponseDto>> StreamingResponseHandlers = [];
    private readonly Dictionary<RequestId, CancellationTokenSource> Requests = [];
    private readonly ISseClientConnection SseClientConnection;

    public SseClient(
        IClientAuthenticatedHttpClient AuthenticationService,
        ISseClientConnection sseClientConnection,
        ServiceId serviceId)
    {
        HttpClient = AuthenticationService;
        this.SseClientConnection = sseClientConnection;
        ServiceId = serviceId;
        Sender = new(this);
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
                var url = $"/SseServiceSubscription/connect/{WebUtility.UrlEncode(ServiceId.Value)}";
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
                            case "ServiceSubscriptionId":
                                if (long.TryParse(eventData, out var serviceSubscriptionId))
                                {
                                    ServiceSubscriptionId = new ServiceSubscriptionId(serviceSubscriptionId);
                                }
                                break;
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
                                    await Receive_StreamingRequest_FromServerAsync(streamingRequest, Cts.Token);
                                break;
                            case "StreamingResponseClientDto":
                                var streamingResponse = JsonSerializer.Deserialize<StreamingResponseClientDto>(eventData);
                                if (streamingResponse != null)
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
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error with SSE: {ex}");
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
        // Quick cleanup
        var keysToRemove = new List<RequestId>();
        foreach (var kvp in Requests)
        {
            if (kvp.Value.IsCancellationRequested)
            {
                keysToRemove.Add(kvp.Key);
            }
        }
        foreach (var key in keysToRemove)
        {
            Requests.Remove(key);
        }

        // Try get and create
        if (Requests.TryGetValue(requestId, out var cts) == false)
            cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(100));
        return cts.Token;
    }
    #endregion

    private async Task Receive_SendRequest_FromServerAsync(SendRequestClientDto message, CancellationToken ct)
    {
        ct = CreateCancellation(message.Routing.RequestId, ct);

        _ = Task.Run(async () =>
        {
            if (message.StateIsChanged)
                await HttpClient.UpdateStateDataAsync(
                    message.StateData,
                    ct);
            await SseClientConnection.SendRequest_ReceivedAsync(
                message.Routing.ServiceId,
                message.Routing.MethodId,
                message.BinaryData,
                ct);
        }, ct);
    }
    private async Task Receive_SendRequestCancelled_FromServerAsync(SendRequestCancelledClientDto message, CancellationToken token)
    {
        if (Requests.TryGetValue(message.Routing.RequestId, out var cts) == false)
            return;

        await cts.CancelAsync();
        cts.Dispose();
    }

    private async Task Receive_InvokeRequest_FromServerAsync(InvokeRequestClientDto message, CancellationToken ct)
    {
        ct = CreateCancellation(message.Routing.RequestId, ct);
        _ = Task.Run(async () =>
        {
            try
            {
                if (message.StateIsChanged)
                    await HttpClient.UpdateStateDataAsync(message.StateData, ct);

                var enumerable = SseClientConnection.InvokeRequest_ReceivedAsync(
                message.Routing.ServiceId,
                message.Routing.MethodId,
                message.BinaryData,
                    ct);

                RegisterAsyncEnumerableArgumentByte(
                    message.Routing,
                    -1,
                    enumerable,
                    ct);

                ct.Register(async () =>
                {
                    await UnRegisterAsyncEnumerableArgument(message.Routing, -1);
                });

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
            }
            catch (Exception ex)
            {
                var stateIsChanged = HttpClient.IsStateDataChanged();
                var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync() : null;
                await Sender.Send_InvokeRequestDone_ToServerAsync(
                    new InvokeRequestDoneClientDto(
                        message.Routing,
                        false,
                        ex.Message,
                        stateIsChanged,
                        stateData
                    ), ct);
            }

        }, ct);
    }
    private async Task Receive_InvokeRequestCancelled_FromServerAsync(InvokeRequestCancelledClientDto message, CancellationToken ct)
    {
        if (Requests.TryGetValue(message.Routing.RequestId, out var cts) == false)
            return;

        await cts.CancelAsync();
        cts.Dispose();
    }

    private async Task Receive_StreamingRequest_FromServerAsync(StreamingRequestClientDto message, CancellationToken ct)
    {
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
                    response.ExceptionMessage,
                    response.BinaryData,
                    stateIsChanged,
                    stateData);
                await Sender.Send_StreamingResponse_ToServerAsync(streamingResponseClient, ct);
            }
        }, ct);
    }
    private async Task Receive_StreamingResponse_FromServerAsync(StreamingResponseClientDto message, CancellationToken ct)
    {
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
                    response.ExceptionMessage,
                    response.BinaryData,
                    stateIsChanged,
                    stateData);
                await Sender.Send_FabricStreamingResponse_ToServerAsync(streamingResponseClient, ct);
            }
        }, ct);
    }
    private async Task Receive_FabricStreamingResponse_FromServerAsync(StreamingResponseClientDto message, CancellationToken ct)
    {
        ct = CreateCancellation(message.Routing.RequestId, ct);
        _ = Task.Run(async () =>
        {
            if (message.StateIsChanged)
                await HttpClient.UpdateStateDataAsync(message.StateData, ct);

            if (StreamingResponseHandlers.TryGetValue(new(message.Routing.RequestId, message.ArgumentIndex, message.StreamId), out var responseHandler))
                responseHandler.Invoke(message);
        }, ct);
    }

    private void RegisterAsyncEnumerableArgumentByte(RoutingDto routing, int argumentIndex, IAsyncEnumerable<byte[]> source, CancellationToken cancellationToken)
    {
        StreamingRequestHandlers.TryAdd(
            new(routing.RequestId, argumentIndex),
            new AsyncEnumerableRegistration<byte[]>(
                async (activeStreams, streamId, cancelled, ct) =>
                {
                    var (enumerator, gate, linkedCts) = activeStreams.GetOrAdd(
                        streamId,
                        _ =>
                        {
                            var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, ct);
                            return new AsyncEnumerableRegistrationInstance<byte[]>(
                                source.GetAsyncEnumerator(linked.Token),
                                new SemaphoreSlim(1, 1),
                                linked);
                        });

                    async Task Cleanup()
                    {
                        activeStreams.TryRemove(streamId, out _);
                        await enumerator.DisposeAsync();
                        linkedCts.Dispose();
                    }

                    var entered = false;
                    try
                    {
                        await gate.WaitAsync(ct);
                        entered = true;

                        var hasNext = !cancelled && await enumerator.MoveNextAsync();

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
                        cancellationToken.IsCancellationRequested)
                    {
                        await Cleanup();

                        var stateIsChanged = HttpClient.IsStateDataChanged();
                        var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync() : null;

                        return new StreamingResponseClientDto(
                            routing,
                            argumentIndex,
                            streamId,
                            true,
                            true,
                            ex.Message,
                            [],
                            stateIsChanged,
                            stateData);
                    }
                    catch (Exception ex)
                    {
                        await Cleanup();

                        var stateIsChanged = HttpClient.IsStateDataChanged();
                        var stateData = stateIsChanged ? await HttpClient.GetStateDataAsync() : null;

                        return new StreamingResponseClientDto(
                            routing,
                            argumentIndex,
                            streamId,
                            true,
                            false,
                            ex.Message,
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
    private async Task UnRegisterAsyncEnumerableArgument(RoutingDto routing, int argumentIndex)
    {
        if (StreamingRequestHandlers.TryRemove(new(routing.RequestId, argumentIndex), out var registration))
        {
            await registration.DisposeAsync();
        }
    }

    public void Dispose()
    {
        Cts.Cancel();
        Cts.Dispose();
        ServiceSubscriptionId = null;
        ClientConnectionId = null;
        FabricConnectionId = null;
        FabricManagerId = null;
    }
}