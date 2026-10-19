using gAPI.Core.Dtos;
using gAPI.Core.Enums;
using gAPI.Core.Helpers;
using gAPI.Core.Ids;
using gAPI.Core.Interfaces;
using gAPI.Core.ServiceBus.Config;
using gAPI.Core.ServiceBus.Helpers;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Concurrent;

namespace gAPI.Core.ServiceBus.RabbitMQ;

public abstract class BaseRabbitMQConnection(
    ILoggerFactory loggerFactory,
    ServiceBusConfig busConfig)
    : IBaseBusConnection, IAsyncDisposable
{
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

    private readonly ILogger<BaseRabbitMQConnection> Logger = loggerFactory.CreateLogger<BaseRabbitMQConnection>();

    private readonly ConcurrentDictionary<RequestArgumentIndexStreamDto, Action<BusStreamingResponseDto>> StreamingResponseHandlers = [];
    private readonly ConcurrentDictionary<RequestArgumentIndexDto, IBusAsyncEnumerableRegistration> StreamingRequestHandlers = [];
    private readonly ConcurrentDictionary<ServiceId, ServiceConsumerRegistration> ServiceConsumers = new();
    private readonly ConcurrentDictionary<RequestId, CancellationTokenSource> _activeRequests = new();


    private readonly SemaphoreSlim SubscriptionLock = new(1, 1);
    private readonly SemaphoreSlim PublishLock = new(1, 1);
    private readonly CancellationTokenSource _lifetimeCts = new();

    private IConnection? _connection;
    private IChannel? _channel;
    private IChannel? _publishChannel;
    private IChannel? _streamingConsumerChannel;

    private string? _streamingQueueName;
    private string? _streamingConsumerTag;

    public bool Initialized { get; private set; }
    public BusConnectionId BusConnectionId { get; private set; } = BusConnectionId.New();

    public async Task TryConnectAsync(CancellationToken ct)
    {
        if (Initialized) return;

        var factory = new ConnectionFactory { Uri = new Uri(busConfig.ConnectionString) };
        _connection = await factory.CreateConnectionAsync(ct);
        _channel = await _connection.CreateChannelAsync(cancellationToken: ct);
        _publishChannel = await _connection.CreateChannelAsync(cancellationToken: ct);

        var result = await _channel.QueueDeclareAsync(
            queue: $"q.autobus.streaming.{BusConnectionId}",
            durable: false, exclusive: true, autoDelete: true, cancellationToken: ct);

        _streamingQueueName = result.QueueName;
        await StartStreamingConsumerAsync(ct);
        Initialized = true;
    }

    public async Task SendFireAndForgetAsync(BusRoutingDto routing, byte[] data, CancellationTokenSource timeoutCts)
    {
        var ct = timeoutCts.Token;
        await TryConnectAsync(ct);

        var sendrequest = new BusSendRequestDto(routing, data);
        var payload = CreateSendRequestData(sendrequest);

        var properties = new BasicProperties
        {
            Type = SendAndForgetRequestType,
            DeliveryMode =
                routing.deliveryMode == DeliveryMode.Transient
                ? DeliveryModes.Transient
                : DeliveryModes.Persistent
        };

        var exchange = routing.To ==
            BusToChoice.ToAll
            ? $"ex.{routing.ServiceId}.fanout"
            : $"ex.{routing.ServiceId}.direct";

        var routingKey = routing.To switch
        {
            BusToChoice.ToAll => "",
            BusToChoice.ToBusConnectionId => $"connection.{routing.ToBusConnectionId}",
            _ => routing.ServiceId.ToString()
        };
        await PublishRawAsync(exchange, routingKey, payload, ct, null, properties);

        ct.Register(async () =>
        {
            try
            {
                var sendrequestcancelled = new BusSendRequestCancelledDto(routing, "Unknown");
                var payload = CreateSendRequestCancelledData(sendrequestcancelled);
                var properties = new BasicProperties
                {
                    Type = SendAndForgetRequestCancelledType,
                    DeliveryMode =
                        routing.deliveryMode == DeliveryMode.Transient
                        ? DeliveryModes.Transient
                        : DeliveryModes.Persistent
                };
                await PublishRawAsync(exchange, routingKey, payload, CancellationToken.None, null, properties);
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
        var replyConsumerChannel = await _connection!.CreateChannelAsync(cancellationToken: ct);

        try
        {
            var replyQueue = await replyConsumerChannel.QueueDeclareAsync(
                queue: "", durable: false, exclusive: true, autoDelete: true, cancellationToken: ct);

            var ackCompletion = new TaskCompletionSource<BusSendRequestAckDto>(TaskCreationOptions.RunContinuationsAsynchronously);

            var consumer = new AsyncEventingBasicConsumer(replyConsumerChannel);
            consumer.ReceivedAsync += (_, ea) =>
            {
                if (ea.BasicProperties.CorrelationId != correlationId || ea.BasicProperties.Type != SendRequestAckType)
                    return Task.CompletedTask;

                try
                {
                    var offset = 0;
                    var ack = BusSendRequestAckDtoSpanSerializer.ReadBusSendRequestAckDto(ea.Body.Span, ref offset);
                    ackCompletion.TrySetResult(ack);
                }
                catch (Exception ex)
                {
                    ackCompletion.TrySetException(ex);
                }
                return Task.CompletedTask;
            };

            await replyConsumerChannel.BasicConsumeAsync(replyQueue.QueueName, autoAck: true, consumer: consumer, cancellationToken: ct);

            await PublishAsync(routing, data, isInvoke: false, ct, replyTo: replyQueue.QueueName, correlationId: correlationId);

            timeoutCts.CancelAfter(routing.TimeOut);
            var ackResult = await ackCompletion.Task.WaitAsync(routing.TimeOut, ct);

            if (ackResult.Exception != null)
                throw new RemoteException(ackResult.Exception);
        }
        finally
        {
            await replyConsumerChannel.CloseAsync();
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
        var replyConsumerChannel = await _connection!.CreateChannelAsync(cancellationToken: ct);

        try
        {
            var replyQueue = await replyConsumerChannel.QueueDeclareAsync(
                queue: "", durable: false, exclusive: true, autoDelete: true, cancellationToken: ct);

            var ackCompletion = new TaskCompletionSource<BusInvokeRequestAckDto>(TaskCreationOptions.RunContinuationsAsynchronously);

            var consumer = new AsyncEventingBasicConsumer(replyConsumerChannel);
            consumer.ReceivedAsync += (_, ea) =>
            {
                timeoutCts.CancelAfter(routing.TimeOut);

                if (ea.BasicProperties.CorrelationId != correlationId || ea.BasicProperties.Type != InvokeRequestAckType)
                    return Task.CompletedTask;

                try
                {
                    var offset = 0;
                    var ack = BusInvokeRequestAckDtoSpanSerializer.ReadBusInvokeRequestAckDto(ea.Body.Span, ref offset);
                    if (ack != null) ackCompletion.TrySetResult(ack);
                }
                catch (Exception ex)
                {
                    ackCompletion.TrySetException(ex);
                }
                return Task.CompletedTask;
            };

            await replyConsumerChannel.BasicConsumeAsync(replyQueue.QueueName, autoAck: true, consumer: consumer, cancellationToken: ct);

            // STAP 1 & 2: => Invoke sturen & wachten op de InvokeAck / InvokeDone
            await PublishAsync(routing, data, isInvoke: true, ct, replyTo: replyQueue.QueueName, correlationId: correlationId);

            timeoutCts.CancelAfter(routing.TimeOut);
            var ackResult = await ackCompletion.Task.WaitAsync(routing.TimeOut, ct);

            if (ackResult.Exception != null)
                throw new RemoteException(ackResult.Exception);

            var streamId = StreamId.New();
            var argumentIndex = -1; // -1 representeert de return-stream van de methode
            var responseKey = new RequestArgumentIndexStreamDto(ackResult.Routing.RequestId, argumentIndex, streamId);

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var responseCompletion = new TaskCompletionSource<BusStreamingResponseDto>(TaskCreationOptions.RunContinuationsAsynchronously);

                    StreamingResponseHandlers[responseKey] = response => responseCompletion.TrySetResult(response);

                    // STAP 3 & 5: => Vraag om volgend item / "Is er nog meer?" via StreamingRequest
                    await SendStreamingRequestAsync(ackResult.Routing, argumentIndex, streamId, cancelled: false, ct);

                    // STAP 4 & 6: <= Wacht op de StreamingResponse van de service
                    timeoutCts.CancelAfter(routing.TimeOut);
                    var responseResult = await responseCompletion.Task.WaitAsync(routing.TimeOut, ct);

                    if (responseResult.ExceptionMessage != null)
                        throw new RemoteException(responseResult.ExceptionMessage);

                    // STAP 6b: De service stuurt Done terug, we sluiten de lus
                    if (responseResult.IsCompleted)
                    {
                        break;
                    }

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
            await replyConsumerChannel.CloseAsync();
        }
    }

    private Task SendStreamingRequestAsync(BusRoutingDto routing, int argumentIndex, StreamId streamId, bool cancelled, CancellationToken ct)
    {
        if (!Initialized || _channel == null || _publishChannel == null || _connection == null)
            throw new InvalidOperationException("Bus connection is not initialized. Call TryConnectAsync first.");

        var request = new BusStreamingRequestDto(routing, argumentIndex, streamId, cancelled);
        var properties = new BasicProperties { Type = StreamingRequestType, ReplyTo = _streamingQueueName };
        byte[] data = CreateStreamingRequestData(request);

        return PublishRawAsync("", $"q.autobus.streaming.{routing.FromBusConnectionId}", data, ct, properties: properties);
    }
    private async Task PublishAsync(BusRoutingDto routing, byte[] data, bool isInvoke, CancellationToken ct, string? replyTo = null, string? correlationId = null)
    {
        if (!Initialized || _channel == null || _publishChannel == null || _connection == null)
            throw new InvalidOperationException("Bus connection is not initialized. Call TryConnectAsync first.");

        byte[] payload;
        if (isInvoke)
        {
            var invokerequest = new BusInvokeRequestDto(routing, data);
            payload = CreateInvokeRequestData(invokerequest);
        }
        else
        {
            var sendrequest = new BusSendRequestDto(routing, data);
            payload = CreateSendRequestData(sendrequest);
        }

        var properties = new BasicProperties
        {
            Type =
                isInvoke
                ? InvokeRequestType
                : SendRequestType,
            DeliveryMode =
                routing.deliveryMode == DeliveryMode.Transient
                ? DeliveryModes.Transient
                : DeliveryModes.Persistent,
            ReplyTo = replyTo,
            CorrelationId = correlationId
        };

        var exchange = routing.To ==
            BusToChoice.ToAll
            ? $"ex.{routing.ServiceId}.fanout"
            : $"ex.{routing.ServiceId}.direct";

        var routingKey = routing.To switch
        {
            BusToChoice.ToAll => "",
            BusToChoice.ToBusConnectionId => $"connection.{routing.ToBusConnectionId}",
            _ => routing.ServiceId.ToString()
        };
        await PublishRawAsync(exchange, routingKey, payload, ct, correlationId, properties);
        ct.Register(async () =>
        {
            try
            {
                byte[] payload;
                if (isInvoke)
                {
                    var invokerequest = new BusInvokeRequestCancelledDto(routing, "Unknown");
                    payload = CreateInvokeRequestCancelledData(invokerequest);
                }
                else
                {
                    var sendrequest = new BusSendRequestCancelledDto(routing, "Unknown");
                    payload = CreateSendRequestCancelledData(sendrequest);
                }
                var properties = new BasicProperties
                {
                    Type =
                        isInvoke
                        ? InvokeRequestCancelledType
                        : SendRequestCancelledType,
                    DeliveryMode =
                        routing.deliveryMode == DeliveryMode.Transient
                        ? DeliveryModes.Transient
                        : DeliveryModes.Persistent,
                    ReplyTo = replyTo,
                    CorrelationId = correlationId
                };
                await PublishRawAsync(exchange, routingKey, payload, CancellationToken.None, null, properties);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to send cancellation message");

            }
        });
    }
    private async Task PublishRawAsync(string exchange, string routingKey, byte[] payload, CancellationToken ct, string? correlationId = null, BasicProperties? properties = null)
    {
        await PublishLock.WaitAsync(ct);
        try
        {
            properties ??= new BasicProperties { CorrelationId = correlationId };
            await _publishChannel!.BasicPublishAsync(exchange, routingKey, false, properties, payload, ct);
        }
        finally
        {
            PublishLock.Release();
        }
    }

    protected async Task SendSubscribeAsync(object service, ServiceId serviceId, CancellationToken ct)
    {
        if (!Initialized || _channel == null || _publishChannel == null || _connection == null)
            throw new InvalidOperationException("Bus verbinding is niet geïnitialiseerd. Roep eerst TryConnectAsync aan.");

        await SubscriptionLock.WaitAsync(ct);
        try
        {
            if (ServiceConsumers.TryGetValue(serviceId, out var existing)) { existing.SubscriptionCount++; return; }
            var registration = new ServiceConsumerRegistration { SubscriptionCount = 1 };
            var directExchange = $"ex.{serviceId}.direct";
            var fanoutExchange = $"ex.{serviceId}.fanout";
            var anyQueue = $"q.{serviceId}.any";
            var allQueue = $"q.{serviceId}.all.{BusConnectionId}";
            var targetedQueue = $"q.{serviceId}.connection.{BusConnectionId}";
            await _channel!.ExchangeDeclareAsync(directExchange, ExchangeType.Direct, durable: true, cancellationToken: ct);
            await _channel.ExchangeDeclareAsync(fanoutExchange, ExchangeType.Fanout, durable: true, cancellationToken: ct);
            await _channel.QueueDeclareAsync(anyQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
            await _channel.QueueBindAsync(anyQueue, directExchange, serviceId.ToString(), cancellationToken: ct);
            await _channel.QueueDeclareAsync(allQueue, durable: false, exclusive: true, autoDelete: true, cancellationToken: ct);
            await _channel.QueueDeclareAsync(targetedQueue, durable: false, exclusive: true, autoDelete: true, cancellationToken: ct);
            await _channel.QueueBindAsync(targetedQueue, directExchange, $"connection.{BusConnectionId}", cancellationToken: ct);
            try
            {
                registration.Consumers.Add(await StartConsumerAsync(anyQueue, ct));
                registration.Consumers.Add(await StartConsumerAsync(allQueue, ct));
                registration.Consumers.Add(await StartConsumerAsync(targetedQueue, ct));
                ServiceConsumers.TryAdd(serviceId, registration);
            }
            catch
            {
                await StopConsumersAsync(registration, CancellationToken.None);
                await _channel.QueueDeleteAsync(allQueue, cancellationToken: CancellationToken.None);
                await _channel.QueueDeleteAsync(targetedQueue, cancellationToken: CancellationToken.None);
                throw;
            }
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
            await StopConsumersAsync(registration, ct);
            await _channel!.QueueDeleteAsync($"q.{serviceId}.all.{BusConnectionId}", cancellationToken: ct);
            await _channel.QueueDeleteAsync($"q.{serviceId}.connection.{BusConnectionId}", cancellationToken: ct);
        }
        finally { SubscriptionLock.Release(); }
    }

    private async Task<ConsumerHandle> StartConsumerAsync(string queueName, CancellationToken ct)
    {
        var consumerChannel = await _connection!.CreateChannelAsync(cancellationToken: ct);
        try
        {
            var consumer = new AsyncEventingBasicConsumer(consumerChannel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var messageType = ea.BasicProperties.Type;
                    switch (messageType)
                    {
                        case SendAndForgetRequestType:
                            var offset1 = 0;
                            var sendRequest1 = BusSendRequestDtoSpanSerializer.ReadBusSendRequestDto(body, ref offset1);
                            await HandleSendAndForgetRequest(sendRequest1, _lifetimeCts.Token);
                            break;
                        case SendAndForgetRequestCancelledType:
                            var offset2 = 0;
                            var sendRequestCancelled2 = BusSendRequestCancelledDtoSpanSerializer.ReadBusSendRequestCancelledDto(body, ref offset2);
                            await HandleSendAndForgetRequestCancelled(sendRequestCancelled2, _lifetimeCts.Token);
                            break;
                        case SendRequestType:
                            var offset3 = 0;
                            var sendRequest3 = BusSendRequestDtoSpanSerializer.ReadBusSendRequestDto(body, ref offset3);
                            var replyTo3 = ea.BasicProperties.ReplyTo;
                            var correlationId3 = ea.BasicProperties.CorrelationId;
                            if (string.IsNullOrWhiteSpace(replyTo3) || string.IsNullOrWhiteSpace(correlationId3))
                                throw new InvalidOperationException("Send request is missing ReplyTo or CorrelationId.");
                            await HandleSendRequestAsync(sendRequest3, replyTo3, correlationId3, _lifetimeCts.Token);
                            break;
                        case SendRequestCancelledType:
                            var offset4 = 0;
                            var sendRequestCancelled4 = BusSendRequestCancelledDtoSpanSerializer.ReadBusSendRequestCancelledDto(body, ref offset4);
                            await HandleSendRequestCancelled(sendRequestCancelled4, _lifetimeCts.Token);
                            break;
                        case InvokeRequestType:
                            var offset5 = 0;
                            var invRequest5 = BusInvokeRequestDtoSpanSerializer.ReadBusInvokeRequestDto(body, ref offset5);
                            var replyTo5 = ea.BasicProperties.ReplyTo;
                            var correlationId5 = ea.BasicProperties.CorrelationId;
                            if (string.IsNullOrWhiteSpace(replyTo5) || string.IsNullOrWhiteSpace(correlationId5))
                                throw new InvalidOperationException("Invoke request is missing ReplyTo or CorrelationId.");
                            await HandleInvokeRequestAsync(invRequest5, replyTo5, correlationId5, _lifetimeCts.Token);
                            break;
                        case InvokeRequestCancelledType:
                            var offset6 = 0;
                            var invokeRequestCancelled6 = BusInvokeRequestCancelledDtoSpanSerializer.ReadBusInvokeRequestCancelledDto(body, ref offset6);
                            await HandleInvokeRequestCancelled(invokeRequestCancelled6, _lifetimeCts.Token);
                            break;
                        default:
                            throw new InvalidOperationException($"Unknown bus message type: {messageType ?? "(null)"}");
                    }
                    await consumerChannel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                }
                catch
                {
                    await consumerChannel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
                }
            };
            var consumerTag = await consumerChannel.BasicConsumeAsync(queueName, autoAck: false, consumer: consumer, cancellationToken: ct);
            return new ConsumerHandle(consumerChannel, consumerTag);
        }
        catch { await consumerChannel.CloseAsync(); throw; }
    }
    private async Task StopConsumersAsync(ServiceConsumerRegistration registration, CancellationToken ct)
    {
        foreach (var consumer in registration.Consumers)
        {
            try { await consumer.Channel.BasicCancelAsync(consumer.ConsumerTag, cancellationToken: ct); }
            finally { await consumer.Channel.CloseAsync(); }
        }
        registration.Consumers.Clear();
    }

    private async Task HandleSendAndForgetRequest(BusSendRequestDto request, CancellationToken ct)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _activeRequests[request.Routing.RequestId] = timeoutCts;
        timeoutCts.CancelAfter(request.Routing.TimeOut);
        ct = timeoutCts.Token;

        try
        {
            await Send_SendRequest_ToServiceAsync(request, timeoutCts.Token);
        }
        finally
        {
            _activeRequests.TryRemove(request.Routing.RequestId, out _);
        }
    }
    private async Task HandleSendAndForgetRequestCancelled(BusSendRequestCancelledDto request, CancellationToken token)
    {
        if (_activeRequests.TryGetValue(request.Routing.RequestId, out var cts) == false)
            return;

        await cts.CancelAsync();
    }
    private async Task HandleSendRequestAsync(BusSendRequestDto request, string replyTo, string correlationId, CancellationToken ct)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _activeRequests[request.Routing.RequestId] = timeoutCts;
        timeoutCts.CancelAfter(request.Routing.TimeOut);
        ct = timeoutCts.Token;

        try
        {
            BusSendRequestAckDto ack;
            try
            {
                var responseRouting = request.Routing with { FromBusConnectionId = BusConnectionId, To = BusToChoice.ToBusConnectionId, ToBusConnectionId = request.Routing.FromBusConnectionId };
                await Send_SendRequest_ToServiceAsync(request, timeoutCts.Token);
                ack = new BusSendRequestAckDto(responseRouting, null);
            }
            catch (Exception ex)
            {
                ack = new BusSendRequestAckDto(request.Routing with { FromBusConnectionId = BusConnectionId, To = BusToChoice.ToBusConnectionId, ToBusConnectionId = request.Routing.FromBusConnectionId }, ex.Message);
            }
            var properties = new BasicProperties { Type = SendRequestAckType, CorrelationId = correlationId };
            var ackData = CreateSendRequestAckData(ack);
            await PublishRawAsync("", replyTo, ackData, timeoutCts.Token, correlationId, properties);
        }
        finally
        {
            _activeRequests.TryRemove(request.Routing.RequestId, out _);
        }
    }
    private async Task HandleSendRequestCancelled(BusSendRequestCancelledDto request, CancellationToken token)
    {
        if (_activeRequests.TryGetValue(request.Routing.RequestId, out var cts) == false)
            return;

        await cts.CancelAsync();
    }
    private async Task HandleInvokeRequestAsync(BusInvokeRequestDto request, string replyTo, string correlationId, CancellationToken ct)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _activeRequests[request.Routing.RequestId] = timeoutCts;
        timeoutCts.CancelAfter(request.Routing.TimeOut);
        ct = timeoutCts.Token;

        try
        {
            BusInvokeRequestAckDto ack;
            try
            {
                var responseRouting = request.Routing with { FromBusConnectionId = BusConnectionId, To = BusToChoice.ToBusConnectionId, ToBusConnectionId = request.Routing.FromBusConnectionId };
                var enumerable = Send_InvokeRequest_ToServiceAsync(request, timeoutCts.Token);
                RegisterAsyncEnumerableArgument<byte[]>(request.Routing, -1, enumerable, static item => item, timeoutCts);
                ack = new BusInvokeRequestAckDto(responseRouting, null);
            }
            catch (Exception ex)
            {
                ack = new BusInvokeRequestAckDto(request.Routing with { FromBusConnectionId = BusConnectionId, To = BusToChoice.ToBusConnectionId, ToBusConnectionId = request.Routing.FromBusConnectionId }, ex.Message);
            }
            var properties = new BasicProperties { Type = InvokeRequestAckType, CorrelationId = correlationId };
            byte[] ackData = CreateInvokeRequestAckData(ack);
            await PublishRawAsync("", replyTo, ackData, timeoutCts.Token, correlationId, properties);
        }
        catch (OperationCanceledException)
        {
            // Gecanceld via de bus! Stuur eventueel een geannuleerde ACK terug
        }
        finally
        {
            _activeRequests.TryRemove(request.Routing.RequestId, out _);
        }
    }
    //private async Task HandleInvokeRequestAsync(BusInvokeRequestDto request, string replyTo, string correlationId, CancellationToken ct)
    //{
    //    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
    //    _activeRequests[request.Routing.RequestId] = timeoutCts;
    //    timeoutCts.CancelAfter(request.Routing.TimeOut);
    //    var innerToken = timeoutCts.Token;

    //    try
    //    {
    //        BusInvokeRequestAckDto ack;
    //        IAsyncEnumerable<byte[]>? serviceStream = null;

    //        try
    //        {
    //            var responseRouting = request.Routing with { FromBusConnectionId = BusConnectionId, To = BusToChoice.ToBusConnectionId, ToBusConnectionId = request.Routing.FromBusConnectionId };

    //            // Haal de rauwe stream op uit de gegenereerde code
    //            serviceStream = Send_InvokeRequest_ToServiceAsync(request, innerToken);

    //            // Registreer deze stream nu met onze veilige wrapper (zie hieronder)
    //            var safeStream = WrapServiceStreamWithExceptionHandling(serviceStream, request);
    //            RegisterAsyncEnumerableArgument<byte[]>(request.Routing, -1, safeStream, static item => item, timeoutCts);

    //            ack = new BusInvokeRequestAckDto(responseRouting, null);
    //        }
    //        catch (Exception ex)
    //        {
    //            ack = new BusInvokeRequestAckDto(request.Routing with { FromBusConnectionId = BusConnectionId, To = BusToChoice.ToBusConnectionId, ToBusConnectionId = request.Routing.FromBusConnectionId }, ex.Message);
    //        }

    //        var properties = new BasicProperties { Type = InvokeRequestAckType, CorrelationId = correlationId };
    //        byte[] ackData = CreateInvokeRequestAckData(ack);
    //        await PublishRawAsync("", replyTo, ackData, innerToken, correlationId, properties);
    //    }
    //    catch (OperationCanceledException)
    //    {
    //        // Gecanceld via de bus!
    //    }
    //    finally
    //    {
    //        _activeRequests.TryRemove(request.Routing.RequestId, out _);
    //    }
    //}

    //// DE NIEUWE WRAPPER METHODE IN DE BASE CLASS
    //private async IAsyncEnumerable<byte[]> WrapServiceStreamWithExceptionHandling(
    //    IAsyncEnumerable<byte[]> sourceStream,
    //    BusInvokeRequestDto request)
    //{
    //    var enumerator = sourceStream.GetAsyncEnumerator();
    //    try
    //    {
    //        while (true)
    //        {
    //            byte[]? item = null;
    //            BusStreamingResponseDto? errDto = null;
    //            try
    //            {
    //                if (!await enumerator.MoveNextAsync()) break;
    //                item = enumerator.Current;
    //            }
    //            catch (Exception ex)
    //            {
    //                // BOOM! De microservice crasht halverwege het yielden.
    //                // We maken een fout-pakket dat de client direct herkent.
    //                errDto = new BusStreamingResponseDto(
    //                    request.Routing,
    //                    ArgumentIndex: -1,
    //                    StreamId.New(),
    //                    IsCompleted: true,
    //                    IsCancelled: false,
    //                    ExceptionMessage: ex.Message,
    //                    BinaryData: []
    //                );
    //            }
    //            if (errDto != null)
    //            {
    //                yield return CreateStreamingResponseData(errDto);
    //                yield break;
    //            }
    //            if (item != null)
    //                yield return item;
    //        }
    //    }
    //    finally
    //    {
    //        await enumerator.DisposeAsync();
    //    }
    //}
    private async Task HandleInvokeRequestCancelled(BusInvokeRequestCancelledDto request, CancellationToken token)
    {
        if (_activeRequests.TryGetValue(request.Routing.RequestId, out var cts) == false)
            return;

        await cts.CancelAsync();
    }

    private async Task StartStreamingConsumerAsync(CancellationToken ct)
    {
        var consumerChannel = await _connection!.CreateChannelAsync(cancellationToken: ct);
        try
        {
            var consumer = new AsyncEventingBasicConsumer(consumerChannel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    switch (ea.BasicProperties.Type)
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
                                response = new BusStreamingResponseDto(request.Routing, request.ArgumentIndex, request.StreamId, IsCompleted: true, IsCancelled: request.Cancelled, ExceptionMessage: request.Cancelled ? null : "De streamingregistratie bestaat niet meer.", BinaryData: []);
                            }
                            if (!string.IsNullOrWhiteSpace(ea.BasicProperties.ReplyTo))
                            {
                                var properties = new BasicProperties { Type = StreamingResponseType };
                                var data = CreateStreamingResponseData(response);
                                await PublishRawAsync("", ea.BasicProperties.ReplyTo, data, _lifetimeCts.Token, properties: properties);
                            }
                            //if (request.ArgumentIndex == -1 && (response.IsCompleted || response.IsCancelled || response.ExceptionMessage != null))
                            //{
                            //    await UnRegisterAsyncEnumerableArgument(request.Routing, -1);
                            //}
                            // FIX: Geldt voor álle argumenten, niet alleen -1! Dit voorkomt memory leaks bij parameters.
                            if (response.IsCompleted || response.IsCancelled || response.ExceptionMessage != null)
                            {
                                await UnRegisterAsyncEnumerableArgument(request.Routing, request.ArgumentIndex);
                            }

                            break;
                        case StreamingResponseType:
                            var offset2 = 0;
                            var streamingResp = BusStreamingResponseDtoSpanSerializer.ReadBusStreamingResponseDto(body, ref offset2);
                            await ReceiveStreamingResponseAsync(streamingResp, _lifetimeCts.Token);
                            break;
                        default:
                            throw new InvalidOperationException($"Unknown streaming message type: {ea.BasicProperties.Type ?? "(null)"}");
                    }
                    await consumerChannel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                }
                catch { await consumerChannel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true); }
            };
            _streamingConsumerTag = await consumerChannel.BasicConsumeAsync(_streamingQueueName!, autoAck: false, consumer: consumer, cancellationToken: ct);
            _streamingConsumerChannel = consumerChannel;
        }
        catch { await consumerChannel.CloseAsync(); throw; }
    }

    private Task ReceiveStreamingResponseAsync(BusStreamingResponseDto response, CancellationToken ct)
    {
        var key = new RequestArgumentIndexStreamDto(response.Routing.RequestId, response.ArgumentIndex, response.StreamId);
        if (StreamingResponseHandlers.TryGetValue(key, out var handler)) handler(response);
        return Task.CompletedTask;
    }

    //protected IAsyncEnumerable<T> RegisterRemoteAsyncEnumerableArgument<T>(BusRoutingDto routing, int argumentIndex, Func<byte[], T> deserializer)
    //{
    //    return new RemoteAsyncEnumerable<T>(
    //        construct: (StreamId streamId, IRemoteAsyncEnumerator<T> enumerator, CancellationToken ct) =>
    //        {
    //            var key = new RequestArgumentIndexStreamDto(routing.RequestId, argumentIndex, streamId);
    //            StreamingResponseHandlers.TryAdd(key, response =>
    //            {
    //                if (response.IsCancelled)
    //                {
    //                    enumerator.Complete(new TaskCanceledException(response.ExceptionMessage));
    //                    return;
    //                }

    //                if (response.ExceptionMessage != null)
    //                {
    //                    enumerator.Complete(new RemoteException(response.ExceptionMessage));
    //                    return;
    //                }

    //                if (response.IsCompleted)
    //                {
    //                    enumerator.Complete();
    //                    return;
    //                }

    //                try
    //                {
    //                    enumerator.Push(deserializer.Invoke(response.BinaryData));
    //                }
    //                catch (Exception ex)
    //                {
    //                    enumerator.Complete(ex);
    //                }
    //            });
    //        },

    //        requestNext: (StreamId streamId, IRemoteAsyncEnumerator<T> enumerator, CancellationToken ct) =>
    //        {
    //            return SendStreamingRequestAsync(
    //                routing,
    //                argumentIndex,
    //                streamId,
    //                false,
    //                ct);
    //        },

    //        cancelled: (StreamId streamId, IRemoteAsyncEnumerator<T> enumerator) =>
    //        {
    //            return SendStreamingRequestAsync(
    //                routing,
    //                argumentIndex,
    //                streamId,
    //                true,
    //                default);
    //        },

    //        dispose: (StreamId streamId) =>
    //        {
    //            var key = new RequestArgumentIndexStreamDto(routing.RequestId, argumentIndex, streamId);
    //            StreamingResponseHandlers.TryRemove(key, out _);
    //        });
    //}

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

    //public void RegisterAsyncEnumerableArgument<T>(BusRoutingDto routing, int argumentIndex, IAsyncEnumerable<T> source, Func<T, byte[]> serializer, CancellationTokenSource cts)
    //{
    //    StreamingRequestHandlers.TryAdd(
    //        new RequestArgumentIndexDto(routing.RequestId, argumentIndex),
    //        new BusAsyncEnumerableRegistration<T>(
    //            async (activeStreams, streamId, cancelled, ct) =>
    //    {
    //        var (enumerator, gate, linkedCts) = activeStreams.GetOrAdd(streamId, _ =>
    //        {
    //            var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, cts.Token);
    //            return new BusAsyncEnumerableRegistrationInstance<T>(source.GetAsyncEnumerator(linked.Token), new SemaphoreSlim(1, 1), linked);
    //        });
    //        async Task Cleanup() { if (activeStreams.TryRemove(streamId, out _)) { await enumerator.DisposeAsync(); linkedCts.Dispose(); } }
    //        var entered = false;
    //        try
    //        {
    //            await gate.WaitAsync(ct); entered = true;
    //            var hasNext = !cancelled && await enumerator.MoveNextAsync();
    //            var response = new BusStreamingResponseDto(routing, argumentIndex, streamId, !hasNext, cancelled, null, hasNext ? serializer(enumerator.Current) : []);
    //            if (!hasNext || cancelled) await Cleanup();
    //            return response;
    //        }
    //        catch (OperationCanceledException ex) { await Cleanup(); return new BusStreamingResponseDto(routing, argumentIndex, streamId, true, true, ex.Message, []); }
    //        catch (Exception ex) { await Cleanup(); return new BusStreamingResponseDto(routing, argumentIndex, streamId, true, false, ex.Message, []); }
    //        finally { if (entered) gate.Release(); }
    //    }));
    //}
    public void RegisterAsyncEnumerableArgument<T>(BusRoutingDto routing, int argumentIndex, IAsyncEnumerable<T> source, Func<T, byte[]> serializer, CancellationTokenSource cts)
    {
        StreamingRequestHandlers.TryAdd(
            new RequestArgumentIndexDto(routing.RequestId, argumentIndex),
            new BusAsyncEnumerableRegistration<T>(
                async (activeStreams, streamId, cancelled, ct) =>
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

                        bool hasNext;
                        T currentItem = default!;
                        string? localExceptionMessage = null;

                        try
                        {
                            // HIER VANGEN WE DE BRON-CRASH OP (Universeel voor parameters én return streams!)
                            hasNext = !cancelled && await enumerator.MoveNextAsync();
                            if (hasNext)
                            {
                                currentItem = enumerator.Current;
                            }
                        }
                        catch (Exception ex)
                        {
                            // De lokale bron-iterator klapt halverwege het yielden uit elkaar
                            hasNext = false;
                            localExceptionMessage = ex.Message;
                        }

                        // Als er een exception was, markeren we de dto direct als Completed met de ExceptionMessage
                        var isCompleted = !hasNext || cancelled || localExceptionMessage != null;

                        var response = new BusStreamingResponseDto(
                            routing,
                            argumentIndex,
                            streamId,
                            IsCompleted: isCompleted,
                            IsCancelled: cancelled,
                            ExceptionMessage: localExceptionMessage,
                            BinaryData: hasNext ? serializer(currentItem) : []
                        );

                        if (isCompleted)
                            await Cleanup();

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
    private byte[] CreateSendRequestAckData(BusSendRequestAckDto ack)
    {
        var offset = 0;
        var length = BusSendRequestAckDtoSpanSerializer.Length(ref offset, ack);
        var ackData = new byte[length];
        offset = 0;
        var span = new Span<byte>(ackData);
        BusSendRequestAckDtoSpanSerializer.Write(ref span, ref offset, ack);
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

    protected abstract Task Send_SendRequest_ToServiceAsync(BusSendRequestDto sendRequest, CancellationToken ct);
    protected abstract IAsyncEnumerable<byte[]> Send_InvokeRequest_ToServiceAsync(BusInvokeRequestDto invokeRequest, CancellationToken ct);

    public async ValueTask DisposeAsync()
    {
        _lifetimeCts.Cancel();
        await SubscriptionLock.WaitAsync();
        try
        {
            foreach (var registration in ServiceConsumers.Values) await StopConsumersAsync(registration, CancellationToken.None);
            ServiceConsumers.Clear();
        }
        finally { SubscriptionLock.Release(); }
        foreach (var registration in StreamingRequestHandlers.Values) await registration.DisposeAsync();
        StreamingRequestHandlers.Clear(); StreamingResponseHandlers.Clear();
        if (_streamingConsumerChannel != null)
        {
            if (_streamingConsumerTag != null) await _streamingConsumerChannel.BasicCancelAsync(_streamingConsumerTag, cancellationToken: CancellationToken.None);
            await _streamingConsumerChannel.CloseAsync();
        }
        if (_publishChannel != null) await _publishChannel.CloseAsync();
        if (_channel != null) await _channel.CloseAsync();
        if (_connection != null) await _connection.CloseAsync();
        _lifetimeCts.Dispose(); SubscriptionLock.Dispose(); PublishLock.Dispose();
        Initialized = false;
        GC.SuppressFinalize(this);
    }

    private sealed class ConsumerHandle(IChannel channel, string consumerTag)
    {
        public IChannel Channel { get; } = channel;
        public string ConsumerTag { get; } = consumerTag;
    }
    private sealed class ServiceConsumerRegistration
    {
        public int SubscriptionCount { get; set; }
        public List<ConsumerHandle> Consumers { get; } = [];
    }
}