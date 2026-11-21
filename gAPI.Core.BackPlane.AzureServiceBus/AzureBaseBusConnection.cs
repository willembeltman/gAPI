using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using gAPI.Core.Dtos;
using gAPI.Core.Enums;
using gAPI.Core.Helpers;
using gAPI.Core.Ids;
using gAPI.Core.Interfaces;
using gAPI.Core.Server.Config;
using gAPI.Core.Server.Dtos;
using gAPI.Core.Server.Helpers;
using gAPI.Core.Server.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace gAPI.Core.BackPlane.AzureServiceBus;

public abstract class AzureBasePlaneConnection(
#pragma warning disable CS9113 // Parameter is unread.
    IHostApplicationLifetime appLifetime,
#pragma warning restore CS9113 // Parameter is unread.
    ILoggerFactory loggerFactory,
    BackPlaneConfig config)
    : IBasePlaneConnection
    , IAsyncDisposable
{
    private const string MessageTypeKey = "gapi-message-type";
    private const string SendAndForgetRequestType = "send-and-forget-request";
    private const string SendAndForgetRequestCancelledType = "send-and-forget-cancelled-request";
    private const string SendRequestType = "send-request";
    private const string SendRequestCancelledType = "send-request-cancelled";
    private const string SendRequestAckType = "send-ack";
    private const string InvokeRequestType = "invoke-request";
    private const string InvokeRequestCancelledType = "invoke-request-cancelled";
    private const string InvokeRequestAckType = "invoke-ack";
    private const string StreamingRequestType = "streaming-request";
    private const string StreamingResponseType = "streaming-response";

    private readonly ILogger<AzureBasePlaneConnection> Logger = loggerFactory.CreateLogger<AzureBasePlaneConnection>();
    public BackPlaneConfig Config { get; } = config;

    private readonly ConcurrentDictionary<RequestArgumentIndexStreamDto, Action<PlaneStreamingResponseDto>> StreamingResponseHandlers = [];
    private readonly ConcurrentDictionary<RequestArgumentIndexDto, IPlaneAsyncEnumerableRegistration> StreamingRequestHandlers = [];
    private readonly ConcurrentDictionary<ServiceId, ServiceConsumerProcessorRegistration> ServiceConsumers = [];
    private readonly ConcurrentDictionary<RequestId, CancellationTokenSourceDispose> _activeRequests = [];

    private readonly SemaphoreSlim SubscriptionLock = new(1, 1);
    private readonly CancellationTokenSource _lifetimeCts = new();

    private ServiceBusClient? _client;
    private ServiceBusAdministrationClient? _adminClient;
    private ServiceBusSender? _topicSender;
    private ServiceBusSessionProcessor? _streamingSessionProcessor;

    private string? _streamingTopicName;
    private string? _streamingSubscriptionName;

    public bool Initialized { get; private set; }
    public PlaneConnectionId PlaneConnectionId { get; private set; } = PlaneConnectionId.New();

    protected abstract Task Send_SendRequest_ToServiceAsync(PlaneSendRequestDto sendRequest, CancellationToken ct);
    protected abstract IAsyncEnumerable<byte[]> Send_InvokeRequest_ToServiceAsync(PlaneInvokeRequestDto invokeRequest, CancellationToken ct);

    public async Task TryConnectAsync(CancellationToken ct)
    {
        if (Initialized) return;

        var clientOptions = new ServiceBusClientOptions();
        var adminOptions = new ServiceBusAdministrationClientOptions();

        // Basis messaging connectionstring (voor poort 5672 / AMQP)
        var messagingConnectionString = Config.ConnectionString;
        var adminConnectionString = Config.ConnectionString;

        // Initialiseer de clients met hun respectievelijke poort-verbindingsreeksen
        _client = new ServiceBusClient(messagingConnectionString, clientOptions);
        _adminClient = new ServiceBusAdministrationClient(adminConnectionString, adminOptions);

        // We gebruiken één centrale topic per microservice-architectuur, of een gedeelde topic voor streaming
        _streamingTopicName = "top.gapi.AutoPlane.streaming";
        _streamingSubscriptionName = $"sub.streaming.{PlaneConnectionId}";

        // Zorg dat de streaming infrastructuur bestaat (Dynamic runtime queues/topics)
        if (!await _adminClient.TopicExistsAsync(_streamingTopicName, ct))
        {
            await _adminClient.CreateTopicAsync(_streamingTopicName, ct);
        }

        if (!await _adminClient.SubscriptionExistsAsync(_streamingTopicName, _streamingSubscriptionName, ct))
        {
            var subOptions = new CreateSubscriptionOptions(_streamingTopicName, _streamingSubscriptionName)
            {
                AutoDeleteOnIdle = TimeSpan.FromMinutes(5), // Ruimt zichzelf netjes op als de verbinding wegvalt
                RequiresSession = true                     // Essentieel voor gerichte RPC streaming routing via SessionId
            };
            await _adminClient.CreateSubscriptionAsync(subOptions, ct);//.CreateSubscriptionOptionsAsync(subOptions, ct);
        }

        _topicSender = _client.CreateSender(_streamingTopicName);

        // Start de streaming sessie processor (luistert specifiek naar berichten voor ONZE PlaneConnectionId)
        var processorOptions = new ServiceBusSessionProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentSessions = 1,
            SessionIds = { PlaneConnectionId.ToString() }
        };
        _streamingSessionProcessor = _client.CreateSessionProcessor(_streamingTopicName, _streamingSubscriptionName, processorOptions);
        _streamingSessionProcessor.ProcessMessageAsync += ProcessStreamingMessageAsync;
        _streamingSessionProcessor.ProcessErrorAsync += ProcessErrorAsync;

        await _streamingSessionProcessor.StartProcessingAsync(ct);
        Initialized = true;
    }

    public async Task SendFireAndForgetAsync(PlaneRoutingDto routing, byte[] data, CancellationToken ct)
    {
        await TryConnectAsync(ct);

        var sendrequest = new PlaneSendRequestDto(routing, data);
        var payload = CreateSendRequestData(sendrequest);

        var message = new ServiceBusMessage(payload) { Subject = routing.ServiceId.ToString() };
        message.ApplicationProperties[MessageTypeKey] = SendAndForgetRequestType;

        var targetTopic = $"top.{routing.ServiceId}";
        await EnsureTopicExistsAsync(targetTopic, ct);

        var sender = _client!.CreateSender(targetTopic);
        await sender.SendMessageAsync(message, ct);

        ct.Register(async () =>
        {
            try
            {
                var sendrequestcancelled = new PlaneSendRequestCancelledDto(routing, "Canceled");
                var cancelPayload = CreateSendRequestCancelledData(sendrequestcancelled);
                var cancelMessage = new ServiceBusMessage(cancelPayload) { Subject = routing.ServiceId.ToString() };
                cancelMessage.ApplicationProperties[MessageTypeKey] = SendAndForgetRequestCancelledType;

                await sender.SendMessageAsync(cancelMessage, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to send cancellation message");
            }
        });
    }
    public async Task SendAsync(PlaneRoutingDto routing, byte[] data, CancellationTokenSourceDispose timeoutCts)
    {
        if (routing.To == PlaneToChoice.ToAll)
        {
            throw new NotSupportedException("Streaming or synchronous RPC calls via ToAll are not supported on the Service Plane. Use ToAny instead.");
        }

        var ct = timeoutCts.Token;
        await TryConnectAsync(ct);

        var correlationId = routing.RequestId.ToString();
        var replyTopic = $"top.gapi.reply.{PlaneConnectionId}";
        var replySub = $"sub.reply.{routing.RequestId}";

        await _adminClient!.CreateTopicAsync(replyTopic, ct);
        var subOptions = new CreateSubscriptionOptions(replyTopic, replySub) { AutoDeleteOnIdle = TimeSpan.FromMinutes(2), RequiresSession = true };
        await _adminClient.CreateSubscriptionAsync(subOptions, ct);

        var ackCompletion = new TaskCompletionSource<PlaneSendRequestDoneDto>(TaskCreationOptions.RunContinuationsAsynchronously);

        var sessionProcessor = _client!.CreateSessionProcessor(replyTopic, replySub, new ServiceBusSessionProcessorOptions
        {
            SessionIds = { PlaneConnectionId.ToString() },
            AutoCompleteMessages = false
        });

        sessionProcessor.ProcessMessageAsync += async args =>
        {
            if (args.Message.CorrelationId == correlationId && args.Message.ApplicationProperties.TryGetValue(MessageTypeKey, out var type) && (string)type == SendRequestAckType)
            {
                try
                {
                    var offset = 0;
                    var ack = PlaneSendRequestDoneDtoSpanSerializer.ReadPlaneSendRequestDoneDto(args.Message.Body.ToArray().AsSpan(), ref offset, Config.MaxPackageSize ?? 512 * 1024);
                    ackCompletion.TrySetResult(ack);
                    await args.CompleteMessageAsync(args.Message);
                }
                catch (Exception ex) { ackCompletion.TrySetException(ex); }
            }
        };
        sessionProcessor.ProcessErrorAsync += ProcessErrorAsync;
        await sessionProcessor.StartProcessingAsync(ct);

        try
        {
            await PublishAsync(routing, data, isInvoke: false, ct, replyTo: replyTopic, correlationId: correlationId);
            timeoutCts.CancelAfter(routing.Timeout);
            var message = await ackCompletion.Task.WaitAsync(routing.Timeout, ct);

            if (message.ExceptionMessage != null) throw new RemoteException(message.ExceptionMessage, message.InnerExceptionMessage);
        }
        finally
        {
            await sessionProcessor.StopProcessingAsync(CancellationToken.None);
            await _adminClient.DeleteSubscriptionAsync(replyTopic, replySub, CancellationToken.None);
            await _adminClient.DeleteTopicAsync(replyTopic, CancellationToken.None);
        }
    }
    public async IAsyncEnumerable<byte[]> InvokeAsync(PlaneRoutingDto routing, byte[] data, CancellationTokenSourceDispose timeoutCts, [EnumeratorCancellation] CancellationToken ct)
    {
        if (routing.To == PlaneToChoice.ToAll)
        {
            throw new NotSupportedException("Streaming or synchronous RPC calls via ToAll are not supported on the Service Plane. Use ToAny instead.");
        }

        //var ct = timeoutCts.Token;
        await TryConnectAsync(ct);

        var correlationId = routing.RequestId.ToString();
        var replyTopic = $"top.gapi.reply.{PlaneConnectionId}";
        var replySub = $"sub.reply.{routing.RequestId}";

        await _adminClient!.CreateTopicAsync(replyTopic, ct);
        var subOptions = new CreateSubscriptionOptions(replyTopic, replySub) { AutoDeleteOnIdle = TimeSpan.FromMinutes(5), RequiresSession = true };
        await _adminClient.CreateSubscriptionAsync(subOptions, ct);
        var ackCompletion = new TaskCompletionSource<PlaneInvokeRequestAckDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        var sessionProcessor = _client!.CreateSessionProcessor(replyTopic, replySub, new ServiceBusSessionProcessorOptions { SessionIds = { PlaneConnectionId.ToString() } });
        sessionProcessor.ProcessMessageAsync += async args =>
        {
            timeoutCts.CancelAfter(routing.Timeout);
            if (args.Message.CorrelationId == correlationId && args.Message.ApplicationProperties.TryGetValue(MessageTypeKey, out var type) && (string)type == InvokeRequestAckType)
            {
                try
                {
                    var offset = 0;
                    var ack = PlaneInvokeRequestAckDtoSpanSerializer.ReadPlaneInvokeRequestAckDto(args.Message.Body.ToArray().AsSpan(), ref offset, Config.MaxPackageSize ?? 512 * 1024);
                    if (ack != null) ackCompletion.TrySetResult(ack);
                    await args.CompleteMessageAsync(args.Message);
                }
                catch (Exception ex) { ackCompletion.TrySetException(ex); }
            }
        };
        sessionProcessor.ProcessErrorAsync += ProcessErrorAsync;
        await sessionProcessor.StartProcessingAsync(ct);
        try
        {
            await PublishAsync(routing, data, isInvoke: true, ct, replyTo: replyTopic, correlationId: correlationId);
            timeoutCts.CancelAfter(routing.Timeout);
            var ackResult = await ackCompletion.Task.WaitAsync(routing.Timeout, ct);

            var streamId = StreamId.New();
            var argumentIndex = -1;
            var responseKey = new RequestArgumentIndexStreamDto(ackResult.Routing.RequestId, argumentIndex, streamId);
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var responseCompletion = new TaskCompletionSource<PlaneStreamingResponseDto>(TaskCreationOptions.RunContinuationsAsynchronously);
                    StreamingResponseHandlers[responseKey] = response => responseCompletion.TrySetResult(response);
                    await SendStreamingRequestAsync(ackResult.Routing, argumentIndex, streamId, cancelled: false, ct);
                    timeoutCts.CancelAfter(routing.Timeout);
                    var message = await responseCompletion.Task.WaitAsync(routing.Timeout, ct);
                    if (message.ExceptionMessage != null) throw new RemoteException(message.ExceptionMessage, message.InnerExceptionMessage);
                    if (message.IsCompleted) break;
                    yield return message.BinaryData;
                }
            }
            finally
            {
                StreamingResponseHandlers.TryRemove(responseKey, out _);
            }
        }
        finally
        {
            await sessionProcessor.StopProcessingAsync(CancellationToken.None);
            await _adminClient.DeleteSubscriptionAsync(replyTopic, replySub, CancellationToken.None);
            await _adminClient.DeleteTopicAsync(replyTopic, CancellationToken.None);
        }
    }

    private async Task SendStreamingRequestAsync(PlaneRoutingDto routing, int argumentIndex, StreamId streamId, bool cancelled, CancellationToken ct)
    {
        if (!Initialized) throw new InvalidOperationException("Plane connection is not initialized. Call TryConnectAsync first.");
        var request = new PlaneStreamingRequestDto(routing, argumentIndex, streamId, cancelled);
        byte[] data = CreateStreamingRequestData(request);
        var message = new ServiceBusMessage(data)
        {
            SessionId = routing.FromPlaneConnectionId.ToString(), // Stuur specifiek naar de vragende bus-instantie
            ReplyTo = _streamingTopicName
        };
        message.ApplicationProperties[MessageTypeKey] = StreamingRequestType;
        await _topicSender!.SendMessageAsync(message, ct);
    }
    private async Task PublishAsync(PlaneRoutingDto routing, byte[] data, bool isInvoke, CancellationToken ct, string? replyTo = null, string? correlationId = null)
    {
        if (!Initialized) throw new InvalidOperationException("Plane connection is not initialized. Call TryConnectAsync first.");
        byte[] payload = isInvoke ? CreateInvokeRequestData(new PlaneInvokeRequestDto(routing, data)) : CreateSendRequestData(new PlaneSendRequestDto(routing, data));
        var message = new ServiceBusMessage(payload)
        {
            CorrelationId = correlationId,
            ReplyTo = replyTo,
            SessionId = PlaneConnectionId.ToString(), // De verwerker stuurt via deze SessionId antwoord terug
            Subject = routing.ServiceId.ToString()
        };
        message.ApplicationProperties[MessageTypeKey] = isInvoke ? InvokeRequestType : SendRequestType;
        var targetTopic = $"top.{routing.ServiceId}";
        await EnsureTopicExistsAsync(targetTopic, ct);
        var sender = _client!.CreateSender(targetTopic);
        await sender.SendMessageAsync(message, ct);
        ct.Register(async () =>
        {
            try
            {
                byte[] cancelPayload = isInvoke ? CreateInvokeRequestCancelledData(new PlaneInvokeRequestCancelledDto(routing, "Canceled")) : CreateSendRequestCancelledData(new PlaneSendRequestCancelledDto(routing, "Canceled"));
                var cancelMessage = new ServiceBusMessage(cancelPayload) { CorrelationId = correlationId, Subject = routing.ServiceId.ToString() };
                cancelMessage.ApplicationProperties[MessageTypeKey] = isInvoke ? InvokeRequestCancelledType : SendRequestCancelledType;
                await sender.SendMessageAsync(cancelMessage, CancellationToken.None);
            }
            catch (Exception ex) { Logger.LogError(ex, "Failed to send cancellation message"); }
        });
    }

    // This is called AFTER the service has been registered for service routing
    // (with the Send_SendRequest_ToServiceAsync / Send_InvokeRequest_ToServiceAsync calls)
    protected async Task SendSubscribeAsync(object service, ServiceId serviceId, CancellationToken ct)
    {
        if (!Initialized) throw new InvalidOperationException("Plane connection is not initialized. Call TryConnectAsync first.");
        await SubscriptionLock.WaitAsync(ct);
        try
        {
            if (ServiceConsumers.TryGetValue(serviceId, out var existing)) { existing.SubscriptionCount++; return; }
            var topicName = $"top.{serviceId}";
            await EnsureTopicExistsAsync(topicName, ct);
            var anySub = $"sub.{serviceId}.any";
            var allSub = $"sub.{serviceId}.all.{PlaneConnectionId}";
            var targetedSub = $"sub.{serviceId}.targeted.{PlaneConnectionId}";
            // 1. Any Queue (Load balanced onder alle actieve instanties)
            if (!await _adminClient!.SubscriptionExistsAsync(topicName, anySub, ct))
                await _adminClient.CreateSubscriptionAsync(topicName, anySub, ct);
            // 2. All Queue (Fanout via unieke tijdelijke subscription)
            var allOptions = new CreateSubscriptionOptions(topicName, allSub) { AutoDeleteOnIdle = TimeSpan.FromMinutes(5) };
            await _adminClient.CreateSubscriptionAsync(allOptions, ct);
            // 3. Targeted Queue (Gerichte RPC)
            var targetedOptions = new CreateSubscriptionOptions(topicName, targetedSub) { AutoDeleteOnIdle = TimeSpan.FromMinutes(5), RequiresSession = true };
            await _adminClient.CreateSubscriptionAsync(targetedOptions, ct);
            var registration = new ServiceConsumerProcessorRegistration { SubscriptionCount = 1 };
            // We gebruiken SQL filters om de ToAll / ToPlaneConnectionId logica na te bootsen op Azure
            registration.Processors.Add(await StartProcessorAsync(topicName, anySub, isSession: false, $"NOT EXISTS([sys].To) OR [sys].To = 'ToAny'", ct));
            registration.Processors.Add(await StartProcessorAsync(topicName, allSub, isSession: false, $"[sys].To = 'ToAll'", ct));
            registration.Processors.Add(await StartProcessorAsync(topicName, targetedSub, isSession: true, null, ct)); // SessionId filtert automatisch op verbinding
            ServiceConsumers.TryAdd(serviceId, registration);
        }
        finally { SubscriptionLock.Release(); }
    }
    // This is called AFTER the service has been unregistered for service routing
    // (with the Send_SendRequest_ToServiceAsync / Send_InvokeRequest_ToServiceAsync calls)
    protected async Task SendUnsubscribeAsync(object service, ServiceId serviceId, CancellationToken ct)
    {
        await SubscriptionLock.WaitAsync(ct);
        try
        {
            if (!ServiceConsumers.TryGetValue(serviceId, out var registration)) return;
            if (--registration.SubscriptionCount > 0) return;
            ServiceConsumers.TryRemove(serviceId, out _);
            foreach (var proc in registration.Processors) await proc.CloseAsync(ct);
            var topicName = $"top.{serviceId}";
            await _adminClient!.DeleteSubscriptionAsync(topicName, $"sub.{serviceId}.all.{PlaneConnectionId}", ct);
            await _adminClient.DeleteSubscriptionAsync(topicName, $"sub.{serviceId}.targeted.{PlaneConnectionId}", ct);
        }
        finally { SubscriptionLock.Release(); }
    }

    private async Task<IProcessorHandle> StartProcessorAsync(string topic, string subscription, bool isSession, string? sqlRule, CancellationToken ct)
    {
        if (sqlRule != null)
        {
            // Verwijder default regel en voeg specifieke gAPI routering toe
            try
            {
                await _adminClient!.DeleteRuleAsync(topic, subscription, RuleProperties.DefaultRuleName, ct);
                await _adminClient.CreateRuleAsync(topic, subscription, new CreateRuleOptions("gapi-routing", new SqlRuleFilter(sqlRule)), ct);
            }
            catch { /* Regel bestaat al of is al aangepast */ }
        }
        if (isSession)
        {
            var p = _client!.CreateSessionProcessor(topic, subscription, new ServiceBusSessionProcessorOptions { AutoCompleteMessages = false });
            p.ProcessMessageAsync += async args =>
            {
                await HandleIncomingMessageAsync(
                args.Message,
                message => args.CompleteMessageAsync(message, ct),
                message => args.AbandonMessageAsync(message, null, ct),
                args.CancellationToken);
            };
            p.ProcessErrorAsync += ProcessErrorAsync;
            await p.StartProcessingAsync(ct);
            return new SessionProcessorHandle(p);
        }
        else
        {
            var p = _client!.CreateProcessor(topic, subscription, new ServiceBusProcessorOptions { AutoCompleteMessages = false });
            p.ProcessMessageAsync += async args =>
            {
                await HandleIncomingMessageAsync(
                args.Message,
                message => args.CompleteMessageAsync(message, ct),
                message => args.AbandonMessageAsync(message, null, ct),
                args.CancellationToken);
            };
            p.ProcessErrorAsync += ProcessErrorAsync;
            await p.StartProcessingAsync(ct);
            return new StandardProcessorHandle(p);
        }
    }
    private async Task HandleIncomingMessageAsync(ServiceBusReceivedMessage message, Func<ServiceBusReceivedMessage, Task> completion, Func<ServiceBusReceivedMessage, Task> abandon, CancellationToken ct)
    {
        try
        {
            var body = message.Body.ToArray();
            if (!message.ApplicationProperties.TryGetValue(MessageTypeKey, out var typeObj) || typeObj is not string messageType) return;
            switch (messageType)
            {
                case SendAndForgetRequestType:
                    var offset1 = 0;
                    await HandleSendAndForgetRequest(PlaneSendRequestDtoSpanSerializer.ReadPlaneSendRequestDto(body, ref offset1, Config.MaxPackageSize ?? 512 * 1024), _lifetimeCts.Token);
                    break;
                case SendAndForgetRequestCancelledType:
                    var offset2 = 0;
                    await HandleSendAndForgetRequestCancelled(PlaneSendRequestCancelledDtoSpanSerializer.ReadPlaneSendRequestCancelledDto(body, ref offset2, Config.MaxPackageSize ?? 512 * 1024), _lifetimeCts.Token);
                    break;
                case SendRequestType:
                    var offset3 = 0;
                    if (string.IsNullOrWhiteSpace(message.ReplyTo) || string.IsNullOrWhiteSpace(message.CorrelationId)) throw new InvalidOperationException("Send request is missing ReplyTo or CorrelationId.");
                    await HandleSendRequestAsync(PlaneSendRequestDtoSpanSerializer.ReadPlaneSendRequestDto(body, ref offset3, Config.MaxPackageSize ?? 512 * 1024), message.ReplyTo, message.CorrelationId, message.SessionId, _lifetimeCts.Token);
                    break;
                case SendRequestCancelledType:
                    var offset4 = 0;
                    await HandleSendRequestCancelled(PlaneSendRequestCancelledDtoSpanSerializer.ReadPlaneSendRequestCancelledDto(body, ref offset4, Config.MaxPackageSize ?? 512 * 1024), _lifetimeCts.Token);
                    break;
                case InvokeRequestType:
                    var offset5 = 0;
                    if (string.IsNullOrWhiteSpace(message.ReplyTo) || string.IsNullOrWhiteSpace(message.CorrelationId)) throw new InvalidOperationException("Invoke request is missing ReplyTo or CorrelationId.");
                    await HandleInvokeRequestAsync(PlaneInvokeRequestDtoSpanSerializer.ReadPlaneInvokeRequestDto(body, ref offset5, Config.MaxPackageSize ?? 512 * 1024), message.ReplyTo, message.CorrelationId, message.SessionId, _lifetimeCts.Token);
                    break;
                case InvokeRequestCancelledType:
                    var offset6 = 0;
                    await HandleInvokeRequestCancelled(PlaneInvokeRequestCancelledDtoSpanSerializer.ReadPlaneInvokeRequestCancelledDto(body, ref offset6, Config.MaxPackageSize ?? 512 * 1024), _lifetimeCts.Token);
                    break;
            }
            await completion(message);
        }
        catch { await abandon(message); }
    }
    private async Task ProcessStreamingMessageAsync(ProcessSessionMessageEventArgs args)
    {
        try
        {
            var body = args.Message.Body.ToArray();
            if (args.Message.ApplicationProperties.TryGetValue(MessageTypeKey, out var typeObj) && typeObj is string messageType)
            {
                switch (messageType)
                {
                    case StreamingRequestType:
                        var offset1 = 0;
                        var request = PlaneStreamingRequestDtoSpanSerializer.ReadPlaneStreamingRequestDto(body, ref offset1, Config.MaxPackageSize ?? 512 * 1024);
                        var key = new RequestArgumentIndexDto(request.Routing.RequestId, request.ArgumentIndex);
                        PlaneStreamingResponseDto response;
                        if (StreamingRequestHandlers.TryGetValue(key, out var registration))
                        {
                            response = await registration.Invoke(request.StreamId, request.Cancelled, _lifetimeCts.Token);
                        }
                        else
                        {
                            response = new PlaneStreamingResponseDto(
                                request.Routing,
                                request.ArgumentIndex,
                                request.StreamId,
                                IsCompleted: true,
                                IsCancelled: request.Cancelled,
                                ExceptionMessage: request.Cancelled ? null : "Streaming registration no longer exists.",
                                InnerExceptionMessage: null,
                                BinaryData: []);
                        }
                        if (!string.IsNullOrWhiteSpace(args.Message.ReplyTo))
                        {
                            var respData = CreateStreamingResponseData(response);
                            var respMessage = new ServiceBusMessage(respData) { SessionId = args.Message.SessionId };
                            respMessage.ApplicationProperties[MessageTypeKey] = StreamingResponseType;
                            var sender = _client!.CreateSender(args.Message.ReplyTo);
                            await sender.SendMessageAsync(respMessage, _lifetimeCts.Token);
                        }
                        if (response.IsCompleted || response.IsCancelled || response.ExceptionMessage != null)
                        {
                            await UnRegisterAsyncEnumerableArgument(request.Routing, request.ArgumentIndex);
                        }
                        break;
                    case StreamingResponseType:
                        var offset2 = 0;
                        var streamingResp = PlaneStreamingResponseDtoSpanSerializer.ReadPlaneStreamingResponseDto(body, ref offset2, Config.MaxPackageSize ?? 512 * 1024);
                        var respKey = new RequestArgumentIndexStreamDto(streamingResp.Routing.RequestId, streamingResp.ArgumentIndex, streamingResp.StreamId);
                        if (StreamingResponseHandlers.TryGetValue(respKey, out var handler)) handler(streamingResp);
                        break;
                }
            }
            await args.CompleteMessageAsync(args.Message);
        }
        catch { await args.AbandonMessageAsync(args.Message); }
    }
    private async Task HandleSendRequestAsync(PlaneSendRequestDto request, string replyTo, string correlationId, string sessionId, CancellationToken ct)
    {
        using var timeoutCts = new CancellationTokenSourceDispose(request.Routing.Timeout, ct);
        _activeRequests[request.Routing.RequestId] = timeoutCts;
        timeoutCts.CancelAfter(request.Routing.Timeout);
        try
        {
            PlaneSendRequestDoneDto ack;
            try
            {
                var responseRouting = request.Routing with { FromPlaneConnectionId = PlaneConnectionId, To = PlaneToChoice.ToPlaneConnectionId, ToPlaneConnectionId = request.Routing.FromPlaneConnectionId };
                await Send_SendRequest_ToServiceAsync(request, timeoutCts.Token);
                ack = new PlaneSendRequestDoneDto(responseRouting, false, null, null);
            }
            catch (Exception ex)
            {
                ack = new PlaneSendRequestDoneDto(
                    request.Routing with
                    {
                        FromPlaneConnectionId = PlaneConnectionId,
                        To = PlaneToChoice.ToPlaneConnectionId,
                        ToPlaneConnectionId = request.Routing.FromPlaneConnectionId
                    },
                    false,
                    ex.Message,
                    ex.InnerException?.Message);
            }
            var ackData = CreateSendRequestAckData(ack);
            var message = new ServiceBusMessage(ackData) { CorrelationId = correlationId, SessionId = sessionId };
            message.ApplicationProperties[MessageTypeKey] = SendRequestAckType;
            var sender = _client!.CreateSender(replyTo);
            await sender.SendMessageAsync(message, timeoutCts.Token);
        }
        finally { _activeRequests.TryRemove(request.Routing.RequestId, out _); }
    }
    private async Task HandleInvokeRequestAsync(PlaneInvokeRequestDto request, string replyTo, string correlationId, string sessionId, CancellationToken ct)
    {
        using var timeoutCts = new CancellationTokenSourceDispose(request.Routing.Timeout, ct);
        _activeRequests[request.Routing.RequestId] = timeoutCts;
        timeoutCts.CancelAfter(request.Routing.Timeout);
        try
        {
            PlaneInvokeRequestAckDto ack;
            try
            {
                var responseRouting = request.Routing with { FromPlaneConnectionId = PlaneConnectionId, To = PlaneToChoice.ToPlaneConnectionId, ToPlaneConnectionId = request.Routing.FromPlaneConnectionId };
                var enumerable = Send_InvokeRequest_ToServiceAsync(request, timeoutCts.Token);
                RegisterAsyncEnumerableArgument<byte[]>(request.Routing, -1, enumerable, static item => item, timeoutCts);
                ack = new PlaneInvokeRequestAckDto(responseRouting, false, null, null);
                var ackData = CreateInvokeRequestAckData(ack);
                var message = new ServiceBusMessage(ackData) { CorrelationId = correlationId, SessionId = sessionId };
                message.ApplicationProperties[MessageTypeKey] = InvokeRequestAckType;
                var sender = _client!.CreateSender(replyTo);
                await sender.SendMessageAsync(message, timeoutCts.Token);
            }
            catch (Exception ex)
            {
                var done = new PlaneInvokeRequestDoneDto(
                    request.Routing with
                    {
                        FromPlaneConnectionId = PlaneConnectionId,
                        To = PlaneToChoice.ToPlaneConnectionId,
                        ToPlaneConnectionId = request.Routing.FromPlaneConnectionId
                    },
                    false,
                    ex.Message,
                    ex.InnerException?.Message);
                var doneData = CreateInvokeRequestDoneData(done);
                var message = new ServiceBusMessage(doneData) { CorrelationId = correlationId, SessionId = sessionId };
                message.ApplicationProperties[MessageTypeKey] = InvokeRequestAckType;
                var sender = _client!.CreateSender(replyTo);
                await sender.SendMessageAsync(message, timeoutCts.Token);
            }
        }
        catch (OperationCanceledException) { }
        finally { _activeRequests.TryRemove(request.Routing.RequestId, out _); }
    }
    private async Task HandleSendAndForgetRequest(PlaneSendRequestDto request, CancellationToken ct)
    {
        using var timeoutCts = new CancellationTokenSourceDispose(request.Routing.Timeout, ct);
        _activeRequests[request.Routing.RequestId] = timeoutCts;
        timeoutCts.CancelAfter(request.Routing.Timeout);
        try { await Send_SendRequest_ToServiceAsync(request, timeoutCts.Token); }
        finally { _activeRequests.TryRemove(request.Routing.RequestId, out _); }
    }
    private async Task HandleSendAndForgetRequestCancelled(PlaneSendRequestCancelledDto request, CancellationToken token)
    {
        if (_activeRequests.TryGetValue(request.Routing.RequestId, out var cts)) await cts.CancelAsync();
    }
    private async Task HandleSendRequestCancelled(PlaneSendRequestCancelledDto request, CancellationToken token)
    {
        if (_activeRequests.TryGetValue(request.Routing.RequestId, out var cts)) await cts.CancelAsync();
    }
    private async Task HandleInvokeRequestCancelled(PlaneInvokeRequestCancelledDto request, CancellationToken token)
    {
        if (_activeRequests.TryGetValue(request.Routing.RequestId, out var cts)) await cts.CancelAsync();
    }

    private async Task EnsureTopicExistsAsync(string topicName, CancellationToken ct)
    {
        if (!await _adminClient!.TopicExistsAsync(topicName, ct)) await _adminClient.CreateTopicAsync(topicName, ct);
    }
    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        Logger.LogError(args.Exception, "Azure Service Plane error in context {Context}", args.ErrorSource);
        return Task.CompletedTask;
    }

    public void RegisterTimeout(PlaneRoutingDto routing, CancellationTokenSourceDispose cts)
    {
        _activeRequests[routing.RequestId] = cts;
    }
    public void UnRegisterTimeout(PlaneRoutingDto routing, CancellationTokenSourceDispose cts)
    {
        _activeRequests.TryRemove(routing.RequestId, out _);
    }

    protected IAsyncEnumerable<T> RegisterRemoteAsyncEnumerableArgument<T>(PlaneRoutingDto routing, int argumentIndex, Func<byte[], T> deserializer, CancellationTokenSourceDispose timeoutCts)
    {
        return new RemoteAsyncEnumerable<T>(
            timeoutCts: timeoutCts,
            construct: (StreamId streamId, IRemoteAsyncEnumerator<T> enumerator, CancellationToken ct) =>
            {
                var key = new RequestArgumentIndexStreamDto(routing.RequestId, argumentIndex, streamId);
                StreamingResponseHandlers.TryAdd(key, message =>
                {
                    // FIX 1: Als de zender actief heeft gecanceld, geef de TaskCanceledException direct door
                    if (message.IsCancelled)
                    {
                        enumerator.Complete(new TaskCanceledException(message.ExceptionMessage ?? "De stream is gecanceld door de zender."));
                        return;
                    }

                    // FIX 2: Als de zender halverwege is gecrasht (ExceptionMessage ingevuld),
                    // breek dan de lokale foreach af met een keiharde RemoteException!
                    if (message.ExceptionMessage != null)
                    {
                        enumerator.Complete(new RemoteException(message.ExceptionMessage, message.InnerExceptionMessage));
                        return;
                    }

                    // Als de zender netjes klaar is
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
                        enumerator.Complete(ex);
                    }
                });
            },

            requestNext: (StreamId streamId, IRemoteAsyncEnumerator<T> enumerator, CancellationToken ct) =>
            {
                return SendStreamingRequestAsync(
                    routing,
                    argumentIndex,
                    streamId,
                    false,
                    ct);
            },

            cancelled: (StreamId streamId, IRemoteAsyncEnumerator<T> enumerator) =>
            {
                return SendStreamingRequestAsync(
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
    public void RegisterAsyncEnumerableArgument<T>(PlaneRoutingDto routing, int argumentIndex, IAsyncEnumerable<T> source, Func<T, byte[]> serializer, CancellationTokenSourceDispose cts)
    {
        StreamingRequestHandlers.TryAdd(new RequestArgumentIndexDto(routing.RequestId, argumentIndex), new PlaneAsyncEnumerableRegistration<T>(async (activeStreams, streamId, cancelled, ct) =>
        {
            var (enumerator, gate, linkedCts) = activeStreams.GetOrAdd(streamId, _ =>
            {
                var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, cts.Token);
                return new PlaneAsyncEnumerableRegistrationInstance<T>(source.GetAsyncEnumerator(linked.Token), new SemaphoreSlim(1, 1), linked);
            });
            async Task Cleanup()
            {
                if (activeStreams.TryRemove(streamId, out _))
                {
                    await enumerator.DisposeAsync(); linkedCts.Dispose();
                }
            }
            var entered = false;
            try
            {
                await gate.WaitAsync(ct); entered = true;
                bool hasNext;
                T currentItem = default!;
                string? localExceptionMessage = null;
                string? localInnerExceptionMessage = null;
                try
                {
                    hasNext = !cancelled && await enumerator.MoveNextAsync();
                    if (hasNext) currentItem = enumerator.Current;
                }
                catch (Exception ex)
                {
                    hasNext = false;
                    localExceptionMessage = ex.Message;
                    localInnerExceptionMessage = ex.InnerException?.Message;
                }
                var isCompleted = !hasNext || cancelled || localExceptionMessage != null;
                var response = new PlaneStreamingResponseDto(
                    routing,
                    argumentIndex,
                    streamId,
                    isCompleted,
                    cancelled,
                    localExceptionMessage,
                    localInnerExceptionMessage,
                    hasNext ? serializer(currentItem) : []);
                if (isCompleted) await Cleanup();
                return response;
            }
            catch (OperationCanceledException ex)
            {
                await Cleanup();
                return new PlaneStreamingResponseDto(
                    routing,
                    argumentIndex,
                    streamId,
                    true,
                    true,
                    ex.Message,
                    ex.InnerException?.Message,
                    []);
            }
            catch (Exception ex)
            {
                await Cleanup();
                return new PlaneStreamingResponseDto(
                    routing,
                    argumentIndex,
                    streamId,
                    true,
                    false,
                    ex.Message,
                    ex.InnerException?.Message,
                    []);
            }
            finally { if (entered) gate.Release(); }
        }));
    }
    public async Task UnRegisterAsyncEnumerableArgument(PlaneRoutingDto routing, int argumentIndex)
    {
        if (StreamingRequestHandlers.TryRemove(new RequestArgumentIndexDto(routing.RequestId, argumentIndex), out var registration)) await registration.DisposeAsync();
    }

    private byte[] CreateSendRequestData(PlaneSendRequestDto sendrequest)
    {
        var offset = 0;
        var length = PlaneSendRequestDtoSpanSerializer.Length(ref offset, sendrequest);
        var payload = new byte[length];
        var span = new Span<byte>(payload);
        offset = 0;
        PlaneSendRequestDtoSpanSerializer.Write(ref span, ref offset, sendrequest);
        return payload;
    }
    private byte[] CreateSendRequestCancelledData(PlaneSendRequestCancelledDto sendrequestcancelled)
    {
        var offset = 0;
        var length = PlaneSendRequestCancelledDtoSpanSerializer.Length(ref offset, sendrequestcancelled);
        var payload = new byte[length];
        var span = new Span<byte>(payload);
        offset = 0;
        PlaneSendRequestCancelledDtoSpanSerializer.Write(ref span, ref offset, sendrequestcancelled);
        return payload;
    }
    private byte[] CreateSendRequestAckData(PlaneSendRequestDoneDto ack)
    {
        var offset = 0;
        var length = PlaneSendRequestDoneDtoSpanSerializer.Length(ref offset, ack);
        var ackData = new byte[length];
        offset = 0;
        var span = new Span<byte>(ackData);
        PlaneSendRequestDoneDtoSpanSerializer.Write(ref span, ref offset, ack);
        return ackData;
    }
    private byte[] CreateInvokeRequestData(PlaneInvokeRequestDto invokerequest)
    {
        var offset = 0;
        var length = PlaneInvokeRequestDtoSpanSerializer.Length(ref offset, invokerequest);
        var payload = new byte[length];
        offset = 0;
        var span = new Span<byte>(payload);
        PlaneInvokeRequestDtoSpanSerializer.Write(ref span, ref offset, invokerequest);
        return payload;
    }
    private byte[] CreateInvokeRequestCancelledData(PlaneInvokeRequestCancelledDto invokerequestcancelled)
    {
        var offset = 0;
        var length = PlaneInvokeRequestCancelledDtoSpanSerializer.Length(ref offset, invokerequestcancelled);
        var payload = new byte[length];
        offset = 0;
        var span = new Span<byte>(payload);
        PlaneInvokeRequestCancelledDtoSpanSerializer.Write(ref span, ref offset, invokerequestcancelled);
        return payload;
    }
    private byte[] CreateInvokeRequestAckData(PlaneInvokeRequestAckDto ack)
    {
        var offset = 0;
        var length = PlaneInvokeRequestAckDtoSpanSerializer.Length(ref offset, ack);
        var data = new byte[length];
        offset = 0;
        var span = new Span<byte>(data);
        PlaneInvokeRequestAckDtoSpanSerializer.Write(ref span, ref offset, ack);
        return data;
    }
    private byte[] CreateInvokeRequestDoneData(PlaneInvokeRequestDoneDto ack)
    {
        var offset = 0;
        var length = PlaneInvokeRequestDoneDtoSpanSerializer.Length(ref offset, ack);
        var data = new byte[length];
        offset = 0;
        var span = new Span<byte>(data);
        PlaneInvokeRequestDoneDtoSpanSerializer.Write(ref span, ref offset, ack);
        return data;
    }
    private byte[] CreateStreamingRequestData(PlaneStreamingRequestDto request)
    {
        var offset = 0;
        var length = PlaneStreamingRequestDtoSpanSerializer.Length(ref offset, request);
        var data = new byte[length];
        offset = 0;
        var span = new Span<byte>(data);
        PlaneStreamingRequestDtoSpanSerializer.Write(ref span, ref offset, request);
        return data;
    }
    private byte[] CreateStreamingResponseData(PlaneStreamingResponseDto response)
    {
        var offset = 0;
        var length = PlaneStreamingResponseDtoSpanSerializer.Length(ref offset, response);
        var data = new byte[length];
        offset = 0;
        var span = new Span<byte>(data);
        PlaneStreamingResponseDtoSpanSerializer.Write(ref span, ref offset, response);
        return data;
    }

    public async ValueTask DisposeAsync()
    {
        _lifetimeCts.Cancel();
        await SubscriptionLock.WaitAsync();
        try
        {
            foreach (var reg in ServiceConsumers.Values) foreach (var proc in reg.Processors) await proc.CloseAsync(CancellationToken.None);
            ServiceConsumers.Clear();
        }
        finally { SubscriptionLock.Release(); }
        foreach (var reg in StreamingRequestHandlers.Values) await reg.DisposeAsync();
        StreamingRequestHandlers.Clear(); StreamingResponseHandlers.Clear();
        if (_streamingSessionProcessor != null) await _streamingSessionProcessor.DisposeAsync();
        if (_topicSender != null) await _topicSender.DisposeAsync();
        if (_client != null) await _client.DisposeAsync();
        _lifetimeCts.Dispose(); SubscriptionLock.Dispose(); System.GC.SuppressFinalize(this);
    }
    private interface IProcessorHandle { Task CloseAsync(CancellationToken ct); }
    private class StandardProcessorHandle(ServiceBusProcessor p) : IProcessorHandle
    {
        public Task CloseAsync(CancellationToken ct) => p.CloseAsync(ct);
    }
    private class SessionProcessorHandle(ServiceBusSessionProcessor p) : IProcessorHandle
    {
        public Task CloseAsync(CancellationToken ct) => p.CloseAsync(ct);
    }
    private class ServiceConsumerProcessorRegistration
    {
        public int SubscriptionCount { get; set; }
        public List<IProcessorHandle> Processors { get; } = [];
    }
}