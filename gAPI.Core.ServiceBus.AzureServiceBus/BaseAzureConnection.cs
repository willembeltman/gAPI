using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using gAPI.Core.Dtos;
using gAPI.Core.Enums;
using gAPI.Core.Helpers;
using gAPI.Core.Ids;
using gAPI.Core.Interfaces;
using gAPI.Core.Server.Helpers;
using gAPI.Core.Server.Config;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace gAPI.Core.ServiceBus.AzureServiceBus;

public abstract class BaseAzureConnection : IBaseBusConnection, IAsyncDisposable
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

    private readonly ILogger<BaseAzureConnection> Logger;
    private readonly ServiceBusConfig BusConfig;

    private readonly ConcurrentDictionary<RequestArgumentIndexStreamDto, Action<BusStreamingResponseDto>> StreamingResponseHandlers = [];
    private readonly ConcurrentDictionary<RequestArgumentIndexDto, IBusAsyncEnumerableRegistration> StreamingRequestHandlers = [];
    private readonly ConcurrentDictionary<ServiceId, ServiceConsumerProcessorRegistration> ServiceConsumers = [];
    private readonly ConcurrentDictionary<RequestId, CancellationTokenSource> _activeRequests = [];

    private readonly SemaphoreSlim SubscriptionLock = new(1, 1);
    private readonly CancellationTokenSource _lifetimeCts = new();

    private ServiceBusClient? _client;
    private ServiceBusAdministrationClient? _adminClient;
    private ServiceBusSender? _topicSender;
    private ServiceBusSessionProcessor? _streamingSessionProcessor;

    private string? _streamingTopicName;
    private string? _streamingSubscriptionName;

    public bool Initialized { get; private set; }
    public BusConnectionId BusConnectionId { get; private set; } = BusConnectionId.New();

    public BaseAzureConnection(ILoggerFactory loggerFactory, ServiceBusConfig busConfig)
    {
        Logger = loggerFactory.CreateLogger<BaseAzureConnection>();
        BusConfig = busConfig;
    }

    protected abstract Task Send_SendRequest_ToServiceAsync(BusSendRequestDto sendRequest, CancellationToken ct);
    protected abstract IAsyncEnumerable<byte[]> Send_InvokeRequest_ToServiceAsync(BusInvokeRequestDto invokeRequest, CancellationToken ct);

    public async Task TryConnectAsync(CancellationToken ct)
    {
        if (Initialized) return;

        var clientOptions = new ServiceBusClientOptions();
        var adminOptions = new ServiceBusAdministrationClientOptions();

        // Basis messaging connectionstring (voor poort 5672 / AMQP)
        var messagingConnectionString = BusConfig.ConnectionString;
        var adminConnectionString = BusConfig.ConnectionString;

        // Initialiseer de clients met hun respectievelijke poort-verbindingsreeksen
        _client = new ServiceBusClient(messagingConnectionString, clientOptions);
        _adminClient = new ServiceBusAdministrationClient(adminConnectionString, adminOptions);

        // We gebruiken één centrale topic per microservice-architectuur, of een gedeelde topic voor streaming
        _streamingTopicName = "top.gapi.autobus.streaming";
        _streamingSubscriptionName = $"sub.streaming.{BusConnectionId}";

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

        // Start de streaming sessie processor (luistert specifiek naar berichten voor ONZE BusConnectionId)
        var processorOptions = new ServiceBusSessionProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentSessions = 1,
            SessionIds = { BusConnectionId.ToString() }
        };
        _streamingSessionProcessor = _client.CreateSessionProcessor(_streamingTopicName, _streamingSubscriptionName, processorOptions);
        _streamingSessionProcessor.ProcessMessageAsync += ProcessStreamingMessageAsync;
        _streamingSessionProcessor.ProcessErrorAsync += ProcessErrorAsync;

        await _streamingSessionProcessor.StartProcessingAsync(ct);
        Initialized = true;
    }

    public async Task SendFireAndForgetAsync(BusRoutingDto routing, byte[] data, CancellationTokenSource timeoutCts)
    {
        var ct = timeoutCts.Token;
        await TryConnectAsync(ct);

        var sendrequest = new BusSendRequestDto(routing, data);
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
                var sendrequestcancelled = new BusSendRequestCancelledDto(routing, "Canceled");
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

    public async Task SendAsync(BusRoutingDto routing, byte[] data, CancellationTokenSource timeoutCts)
    {
        if (routing.To == BusToChoice.ToAll)
        {
            throw new NotSupportedException("Streaming or synchronous RPC calls via ToAll are not supported on the Service Bus. Use ToAny instead.");
        }

        var ct = timeoutCts.Token;
        await TryConnectAsync(ct);

        var correlationId = routing.RequestId.ToString();
        var replyTopic = $"top.gapi.reply.{BusConnectionId}";
        var replySub = $"sub.reply.{routing.RequestId}";

        await _adminClient!.CreateTopicAsync(replyTopic, ct);
        var subOptions = new CreateSubscriptionOptions(replyTopic, replySub) { AutoDeleteOnIdle = TimeSpan.FromMinutes(2), RequiresSession = true };
        await _adminClient.CreateSubscriptionAsync(subOptions, ct);

        var ackCompletion = new TaskCompletionSource<BusSendRequestDoneDto>(TaskCreationOptions.RunContinuationsAsynchronously);

        var sessionProcessor = _client!.CreateSessionProcessor(replyTopic, replySub, new ServiceBusSessionProcessorOptions
        {
            SessionIds = { BusConnectionId.ToString() },
            AutoCompleteMessages = false
        });

        sessionProcessor.ProcessMessageAsync += async args =>
        {
            if (args.Message.CorrelationId == correlationId && args.Message.ApplicationProperties.TryGetValue(MessageTypeKey, out var type) && (string)type == SendRequestAckType)
            {
                try
                {
                    var offset = 0;
                    var ack = BusSendRequestDoneDtoSpanSerializer.ReadBusSendRequestDoneDto(args.Message.Body.ToArray().AsSpan(), ref offset);
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
            var ackResult = await ackCompletion.Task.WaitAsync(routing.Timeout, ct);

            if (ackResult.ExceptionMessage != null) throw new RemoteException(ackResult.ExceptionMessage);
        }
        finally
        {
            await sessionProcessor.StopProcessingAsync(CancellationToken.None);
            await _adminClient.DeleteSubscriptionAsync(replyTopic, replySub, CancellationToken.None);
            await _adminClient.DeleteTopicAsync(replyTopic, CancellationToken.None);
        }
    }

    public async IAsyncEnumerable<byte[]> InvokeAsync(BusRoutingDto routing, byte[] data, CancellationTokenSource timeoutCts)
    {
        if (routing.To == BusToChoice.ToAll)
        {
            throw new NotSupportedException("Streaming or synchronous RPC calls via ToAll are not supported on the Service Bus. Use ToAny instead.");
        }

        var ct = timeoutCts.Token;
        await TryConnectAsync(ct);

        var correlationId = routing.RequestId.ToString();
        var replyTopic = $"top.gapi.reply.{BusConnectionId}";
        var replySub = $"sub.reply.{routing.RequestId}";

        await _adminClient!.CreateTopicAsync(replyTopic, ct);
        var subOptions = new CreateSubscriptionOptions(replyTopic, replySub) { AutoDeleteOnIdle = TimeSpan.FromMinutes(5), RequiresSession = true };
        await _adminClient.CreateSubscriptionAsync(subOptions, ct);
        var ackCompletion = new TaskCompletionSource<BusInvokeRequestAckDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        var sessionProcessor = _client!.CreateSessionProcessor(replyTopic, replySub, new ServiceBusSessionProcessorOptions { SessionIds = { BusConnectionId.ToString() } });
        sessionProcessor.ProcessMessageAsync += async args =>
        {
            timeoutCts.CancelAfter(routing.Timeout);
            if (args.Message.CorrelationId == correlationId && args.Message.ApplicationProperties.TryGetValue(MessageTypeKey, out var type) && (string)type == InvokeRequestAckType)
            {
                try
                {
                    var offset = 0;
                    var ack = BusInvokeRequestAckDtoSpanSerializer.ReadBusInvokeRequestAckDto(args.Message.Body.ToArray().AsSpan(), ref offset);
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
                    var responseCompletion = new TaskCompletionSource<BusStreamingResponseDto>(TaskCreationOptions.RunContinuationsAsynchronously);
                    StreamingResponseHandlers[responseKey] = response => responseCompletion.TrySetResult(response);
                    await SendStreamingRequestAsync(ackResult.Routing, argumentIndex, streamId, cancelled: false, ct);
                    timeoutCts.CancelAfter(routing.Timeout);
                    var responseResult = await responseCompletion.Task.WaitAsync(routing.Timeout, ct);
                    if (responseResult.ExceptionMessage != null) throw new RemoteException(responseResult.ExceptionMessage);
                    if (responseResult.IsCompleted) break;
                    yield return responseResult.BinaryData;
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
    private async Task SendStreamingRequestAsync(BusRoutingDto routing, int argumentIndex, StreamId streamId, bool cancelled, CancellationToken ct)
    {
        if (!Initialized) throw new InvalidOperationException("Bus connection is not initialized. Call TryConnectAsync first.");
        var request = new BusStreamingRequestDto(routing, argumentIndex, streamId, cancelled);
        byte[] data = CreateStreamingRequestData(request);
        var message = new ServiceBusMessage(data)
        {
            SessionId = routing.FromBusConnectionId.ToString(), // Stuur specifiek naar de vragende bus-instantie
            ReplyTo = _streamingTopicName
        };
        message.ApplicationProperties[MessageTypeKey] = StreamingRequestType;
        await _topicSender!.SendMessageAsync(message, ct);
    }
    private async Task PublishAsync(BusRoutingDto routing, byte[] data, bool isInvoke, CancellationToken ct, string? replyTo = null, string? correlationId = null)
    {
        if (!Initialized) throw new InvalidOperationException("Bus connection is not initialized. Call TryConnectAsync first.");
        byte[] payload = isInvoke ? CreateInvokeRequestData(new BusInvokeRequestDto(routing, data)) : CreateSendRequestData(new BusSendRequestDto(routing, data));
        var message = new ServiceBusMessage(payload)
        {
            CorrelationId = correlationId,
            ReplyTo = replyTo,
            SessionId = BusConnectionId.ToString(), // De verwerker stuurt via deze SessionId antwoord terug
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
                byte[] cancelPayload = isInvoke ? CreateInvokeRequestCancelledData(new BusInvokeRequestCancelledDto(routing, "Canceled")) : CreateSendRequestCancelledData(new BusSendRequestCancelledDto(routing, "Canceled"));
                var cancelMessage = new ServiceBusMessage(cancelPayload) { CorrelationId = correlationId, Subject = routing.ServiceId.ToString() };
                cancelMessage.ApplicationProperties[MessageTypeKey] = isInvoke ? InvokeRequestCancelledType : SendRequestCancelledType;
                await sender.SendMessageAsync(cancelMessage, CancellationToken.None);
            }
            catch (Exception ex) { Logger.LogError(ex, "Failed to send cancellation message"); }
        });
    }
    protected async Task SendSubscribeAsync(object service, ServiceId serviceId, CancellationToken ct)
    {
        if (!Initialized) throw new InvalidOperationException("Bus connection is not initialized. Call TryConnectAsync first.");
        await SubscriptionLock.WaitAsync(ct);
        try
        {
            if (ServiceConsumers.TryGetValue(serviceId, out var existing)) { existing.SubscriptionCount++; return; }
            var topicName = $"top.{serviceId}";
            await EnsureTopicExistsAsync(topicName, ct);
            var anySub = $"sub.{serviceId}.any";
            var allSub = $"sub.{serviceId}.all.{BusConnectionId}";
            var targetedSub = $"sub.{serviceId}.targeted.{BusConnectionId}";
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
            // We gebruiken SQL filters om de ToAll / ToBusConnectionId logica na te bootsen op Azure
            registration.Processors.Add(await StartProcessorAsync(topicName, anySub, isSession: false, $"NOT EXISTS([sys].To) OR [sys].To = 'ToAny'", ct));
            registration.Processors.Add(await StartProcessorAsync(topicName, allSub, isSession: false, $"[sys].To = 'ToAll'", ct));
            registration.Processors.Add(await StartProcessorAsync(topicName, targetedSub, isSession: true, null, ct)); // SessionId filtert automatisch op verbinding
            ServiceConsumers.TryAdd(serviceId, registration);
        }
        finally { SubscriptionLock.Release(); }
    }
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
            await _adminClient!.DeleteSubscriptionAsync(topicName, $"sub.{serviceId}.all.{BusConnectionId}", ct);
            await _adminClient.DeleteSubscriptionAsync(topicName, $"sub.{serviceId}.targeted.{BusConnectionId}", ct);
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
    private async Task HandleIncomingMessageAsync(ServiceBusReceivedMessage message, Func<ServiceBusReceivedMessage, Task> complete, Func<ServiceBusReceivedMessage, Task> abandon, CancellationToken ct)
    {
        try
        {
            var body = message.Body.ToArray();
            if (!message.ApplicationProperties.TryGetValue(MessageTypeKey, out var typeObj) || typeObj is not string messageType) return;
            switch (messageType)
            {
                case SendAndForgetRequestType:
                    var offset1 = 0;
                    await HandleSendAndForgetRequest(BusSendRequestDtoSpanSerializer.ReadBusSendRequestDto(body, ref offset1), _lifetimeCts.Token);
                    break;
                case SendAndForgetRequestCancelledType:
                    var offset2 = 0;
                    await HandleSendAndForgetRequestCancelled(BusSendRequestCancelledDtoSpanSerializer.ReadBusSendRequestCancelledDto(body, ref offset2), _lifetimeCts.Token);
                    break;
                case SendRequestType:
                    var offset3 = 0;
                    if (string.IsNullOrWhiteSpace(message.ReplyTo) || string.IsNullOrWhiteSpace(message.CorrelationId)) throw new InvalidOperationException("Send request is missing ReplyTo or CorrelationId.");
                    await HandleSendRequestAsync(BusSendRequestDtoSpanSerializer.ReadBusSendRequestDto(body, ref offset3), message.ReplyTo, message.CorrelationId, message.SessionId, _lifetimeCts.Token);
                    break;
                case SendRequestCancelledType:
                    var offset4 = 0;
                    await HandleSendRequestCancelled(BusSendRequestCancelledDtoSpanSerializer.ReadBusSendRequestCancelledDto(body, ref offset4), _lifetimeCts.Token);
                    break;
                case InvokeRequestType:
                    var offset5 = 0;
                    if (string.IsNullOrWhiteSpace(message.ReplyTo) || string.IsNullOrWhiteSpace(message.CorrelationId)) throw new InvalidOperationException("Invoke request is missing ReplyTo or CorrelationId.");
                    await HandleInvokeRequestAsync(BusInvokeRequestDtoSpanSerializer.ReadBusInvokeRequestDto(body, ref offset5), message.ReplyTo, message.CorrelationId, message.SessionId, _lifetimeCts.Token);
                    break;
                case InvokeRequestCancelledType:
                    var offset6 = 0;
                    await HandleInvokeRequestCancelled(BusInvokeRequestCancelledDtoSpanSerializer.ReadBusInvokeRequestCancelledDto(body, ref offset6), _lifetimeCts.Token);
                    break;
            }
            await complete(message);
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
                        var request = BusStreamingRequestDtoSpanSerializer.ReadBusStreamingRequestDto(body, ref offset1);
                        var key = new RequestArgumentIndexDto(request.Routing.RequestId, request.ArgumentIndex);
                        BusStreamingResponseDto response;
                        if (StreamingRequestHandlers.TryGetValue(key, out var registration))
                        {
                            response = await registration.Invoke(request.StreamId, request.Cancelled, _lifetimeCts.Token);
                        }
                        else
                        {
                            response = new BusStreamingResponseDto(request.Routing, request.ArgumentIndex, request.StreamId, IsCompleted: true, IsCancelled: request.Cancelled, ExceptionMessage: request.Cancelled ? null : "Streaming registration no longer exists.", BinaryData: []);
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
                        var streamingResp = BusStreamingResponseDtoSpanSerializer.ReadBusStreamingResponseDto(body, ref offset2);
                        var respKey = new RequestArgumentIndexStreamDto(streamingResp.Routing.RequestId, streamingResp.ArgumentIndex, streamingResp.StreamId);
                        if (StreamingResponseHandlers.TryGetValue(respKey, out var handler)) handler(streamingResp);
                        break;
                }
            }
            await args.CompleteMessageAsync(args.Message);
        }
        catch { await args.AbandonMessageAsync(args.Message); }
    }
    private async Task HandleSendRequestAsync(BusSendRequestDto request, string replyTo, string correlationId, string sessionId, CancellationToken ct)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _activeRequests[request.Routing.RequestId] = timeoutCts;
        timeoutCts.CancelAfter(request.Routing.Timeout);
        try
        {
            BusSendRequestDoneDto ack;
            try
            {
                var responseRouting = request.Routing with { FromBusConnectionId = BusConnectionId, To = BusToChoice.ToBusConnectionId, ToBusConnectionId = request.Routing.FromBusConnectionId };
                await Send_SendRequest_ToServiceAsync(request, timeoutCts.Token);
                ack = new BusSendRequestDoneDto(responseRouting, false, null);
            }
            catch (Exception ex)
            {
                ack = new BusSendRequestDoneDto(request.Routing with { FromBusConnectionId = BusConnectionId, To = BusToChoice.ToBusConnectionId, ToBusConnectionId = request.Routing.FromBusConnectionId }, false, ex.Message);
            }
            var ackData = CreateSendRequestAckData(ack);
            var message = new ServiceBusMessage(ackData) { CorrelationId = correlationId, SessionId = sessionId };
            message.ApplicationProperties[MessageTypeKey] = SendRequestAckType;
            var sender = _client!.CreateSender(replyTo);
            await sender.SendMessageAsync(message, timeoutCts.Token);
        }
        finally { _activeRequests.TryRemove(request.Routing.RequestId, out _); }
    }
    private async Task HandleInvokeRequestAsync(BusInvokeRequestDto request, string replyTo, string correlationId, string sessionId, CancellationToken ct)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _activeRequests[request.Routing.RequestId] = timeoutCts;
        timeoutCts.CancelAfter(request.Routing.Timeout);
        try
        {
            BusInvokeRequestAckDto ack;
            try
            {
                var responseRouting = request.Routing with { FromBusConnectionId = BusConnectionId, To = BusToChoice.ToBusConnectionId, ToBusConnectionId = request.Routing.FromBusConnectionId };
                var enumerable = Send_InvokeRequest_ToServiceAsync(request, timeoutCts.Token);
                RegisterAsyncEnumerableArgument<byte[]>(request.Routing, -1, enumerable, static item => item, timeoutCts);
                ack = new BusInvokeRequestAckDto(responseRouting);
                var ackData = CreateInvokeRequestAckData(ack);
                var message = new ServiceBusMessage(ackData) { CorrelationId = correlationId, SessionId = sessionId };
                message.ApplicationProperties[MessageTypeKey] = InvokeRequestAckType;
                var sender = _client!.CreateSender(replyTo);
                await sender.SendMessageAsync(message, timeoutCts.Token);
            }
            catch (Exception ex)
            {
                var done = new BusInvokeRequestDoneDto(request.Routing with { FromBusConnectionId = BusConnectionId, To = BusToChoice.ToBusConnectionId, ToBusConnectionId = request.Routing.FromBusConnectionId }, false, ex.Message);
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
    private async Task HandleSendAndForgetRequest(BusSendRequestDto request, CancellationToken ct)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _activeRequests[request.Routing.RequestId] = timeoutCts;
        timeoutCts.CancelAfter(request.Routing.Timeout);
        try { await Send_SendRequest_ToServiceAsync(request, timeoutCts.Token); }
        finally { _activeRequests.TryRemove(request.Routing.RequestId, out _); }
    }
    private async Task HandleSendAndForgetRequestCancelled(BusSendRequestCancelledDto request, CancellationToken token)
    {
        if (_activeRequests.TryGetValue(request.Routing.RequestId, out var cts)) await cts.CancelAsync();
    }
    private async Task HandleSendRequestCancelled(BusSendRequestCancelledDto request, CancellationToken token)
    {
        if (_activeRequests.TryGetValue(request.Routing.RequestId, out var cts)) await cts.CancelAsync();
    }
    private async Task HandleInvokeRequestCancelled(BusInvokeRequestCancelledDto request, CancellationToken token)
    {
        if (_activeRequests.TryGetValue(request.Routing.RequestId, out var cts)) await cts.CancelAsync();
    }

    private async Task EnsureTopicExistsAsync(string topicName, CancellationToken ct)
    {
        if (!await _adminClient!.TopicExistsAsync(topicName, ct)) await _adminClient.CreateTopicAsync(topicName, ct);
    }
    private Task ProcessErrorAsync(ProcessErrorEventArgs args) { Logger.LogError(args.Exception, "Azure Service Bus error in context {Context}", args.ErrorSource); return Task.CompletedTask; }
    //private Task ProcessErrorAsync(ProcessSessionErrorEventArgs args) { Logger.LogError(args.Exception, "Azure Service Bus Session error in entity {Entity}", args.EntityPath); return Task.CompletedTask; }


    protected IAsyncEnumerable<T> RegisterRemoteAsyncEnumerableArgument<T>(BusRoutingDto routing, int argumentIndex, Func<byte[], T> deserializer)
    {
        return new RemoteAsyncEnumerable<T>(
            construct: (StreamId streamId, IRemoteAsyncEnumerator<T> enumerator, CancellationToken ct) =>
            {
                var key = new RequestArgumentIndexStreamDto(routing.RequestId, argumentIndex, streamId);
                StreamingResponseHandlers.TryAdd(key, response =>
                {
                    // FIX 1: Als de zender actief heeft gecanceld, geef de TaskCanceledException direct door
                    if (response.IsCancelled)
                    {
                        enumerator.Complete(new TaskCanceledException(response.ExceptionMessage ?? "De stream is gecanceld door de zender."));
                        return;
                    }

                    // FIX 2: Als de zender halverwege is gecrasht (ExceptionMessage ingevuld),
                    // breek dan de lokale foreach af met een keiharde RemoteException!
                    if (response.ExceptionMessage != null)
                    {
                        enumerator.Complete(new RemoteException(response.ExceptionMessage));
                        return;
                    }

                    // Als de zender netjes klaar is
                    if (response.IsCompleted)
                    {
                        enumerator.Complete();
                        return;
                    }

                    try
                    {
                        enumerator.Push(deserializer.Invoke(response.BinaryData));
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


    public void RegisterAsyncEnumerableArgument<T>(BusRoutingDto routing, int argumentIndex, IAsyncEnumerable<T> source, Func<T, byte[]> serializer, CancellationTokenSource cts)
    {
        StreamingRequestHandlers.TryAdd(new RequestArgumentIndexDto(routing.RequestId, argumentIndex), new BusAsyncEnumerableRegistration<T>(async (activeStreams, streamId, cancelled, ct) =>
        {
            var (enumerator, gate, linkedCts) = activeStreams.GetOrAdd(streamId, _ =>
            {
                var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, cts.Token);
                return new BusAsyncEnumerableRegistrationInstance<T>(source.GetAsyncEnumerator(linked.Token), new SemaphoreSlim(1, 1), linked);
            });
            async Task Cleanup() { if (activeStreams.TryRemove(streamId, out _)) { await enumerator.DisposeAsync(); linkedCts.Dispose(); } }
            var entered = false;
            try
            {
                await gate.WaitAsync(ct); entered = true;
                bool hasNext; T currentItem = default!; string? localExceptionMessage = null;
                try
                {
                    hasNext = !cancelled && await enumerator.MoveNextAsync();
                    if (hasNext) currentItem = enumerator.Current;
                }
                catch (Exception ex) { hasNext = false; localExceptionMessage = ex.Message; }
                var isCompleted = !hasNext || cancelled || localExceptionMessage != null;
                var response = new BusStreamingResponseDto(routing, argumentIndex, streamId, isCompleted, cancelled, localExceptionMessage, hasNext ? serializer(currentItem) : []);
                if (isCompleted) await Cleanup();
                return response;
            }
            catch (OperationCanceledException ex) { await Cleanup(); return new BusStreamingResponseDto(routing, argumentIndex, streamId, true, true, ex.Message, []); }
            catch (Exception ex) { await Cleanup(); return new BusStreamingResponseDto(routing, argumentIndex, streamId, true, false, ex.Message, []); }
            finally { if (entered) gate.Release(); }
        }));
    }
    public async Task UnRegisterAsyncEnumerableArgument(BusRoutingDto routing, int argumentIndex)
    {
        if (StreamingRequestHandlers.TryRemove(new RequestArgumentIndexDto(routing.RequestId, argumentIndex), out var registration)) await registration.DisposeAsync();
    }

    private byte[] CreateSendRequestData(BusSendRequestDto sendrequest)
    {
        var offset = 0;
        var length = BusSendRequestDtoSpanSerializer.Length(ref offset, sendrequest);
        var payload = new byte[length];
        var span = new Span<byte>(payload);
        offset = 0;
        BusSendRequestDtoSpanSerializer.Write(ref span, ref offset, sendrequest);
        return payload;
    }
    private byte[] CreateSendRequestCancelledData(BusSendRequestCancelledDto sendrequestcancelled)
    {
        var offset = 0;
        var length = BusSendRequestCancelledDtoSpanSerializer.Length(ref offset, sendrequestcancelled);
        var payload = new byte[length];
        var span = new Span<byte>(payload);
        offset = 0;
        BusSendRequestCancelledDtoSpanSerializer.Write(ref span, ref offset, sendrequestcancelled);
        return payload;
    }
    private byte[] CreateSendRequestAckData(BusSendRequestDoneDto ack)
    {
        var offset = 0;
        var length = BusSendRequestDoneDtoSpanSerializer.Length(ref offset, ack);
        var ackData = new byte[length];
        offset = 0;
        var span = new Span<byte>(ackData);
        BusSendRequestDoneDtoSpanSerializer.Write(ref span, ref offset, ack);
        return ackData;
    }
    private byte[] CreateInvokeRequestData(BusInvokeRequestDto invokerequest)
    {
        var offset = 0;
        var length = BusInvokeRequestDtoSpanSerializer.Length(ref offset, invokerequest);
        var payload = new byte[length];
        offset = 0;
        var span = new Span<byte>(payload);
        BusInvokeRequestDtoSpanSerializer.Write(ref span, ref offset, invokerequest);
        return payload;
    }
    private byte[] CreateInvokeRequestCancelledData(BusInvokeRequestCancelledDto invokerequestcancelled)
    {
        var offset = 0;
        var length = BusInvokeRequestCancelledDtoSpanSerializer.Length(ref offset, invokerequestcancelled);
        var payload = new byte[length];
        offset = 0;
        var span = new Span<byte>(payload);
        BusInvokeRequestCancelledDtoSpanSerializer.Write(ref span, ref offset, invokerequestcancelled);
        return payload;
    }
    private byte[] CreateInvokeRequestAckData(BusInvokeRequestAckDto ack)
    {
        var offset = 0;
        var length = BusInvokeRequestAckDtoSpanSerializer.Length(ref offset, ack);
        var data = new byte[length];
        offset = 0;
        var span = new Span<byte>(data);
        BusInvokeRequestAckDtoSpanSerializer.Write(ref span, ref offset, ack);
        return data;
    }
    private byte[] CreateInvokeRequestDoneData(BusInvokeRequestDoneDto ack)
    {
        var offset = 0;
        var length = BusInvokeRequestDoneDtoSpanSerializer.Length(ref offset, ack);
        var data = new byte[length];
        offset = 0;
        var span = new Span<byte>(data);
        BusInvokeRequestDoneDtoSpanSerializer.Write(ref span, ref offset, ack);
        return data;
    }
    private byte[] CreateStreamingRequestData(BusStreamingRequestDto request)
    {
        var offset = 0;
        var length = BusStreamingRequestDtoSpanSerializer.Length(ref offset, request);
        var data = new byte[length];
        offset = 0;
        var span = new Span<byte>(data);
        BusStreamingRequestDtoSpanSerializer.Write(ref span, ref offset, request);
        return data;
    }
    private byte[] CreateStreamingResponseData(BusStreamingResponseDto response)
    {
        var offset = 0;
        var length = BusStreamingResponseDtoSpanSerializer.Length(ref offset, response);
        var data = new byte[length];
        offset = 0;
        var span = new Span<byte>(data);
        BusStreamingResponseDtoSpanSerializer.Write(ref span, ref offset, response);
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