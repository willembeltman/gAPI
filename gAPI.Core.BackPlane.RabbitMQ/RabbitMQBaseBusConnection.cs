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
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Concurrent;

namespace gAPI.Core.BackPlane.RabbitMQ;

public abstract class RabbitMQBasePlaneConnection(
#pragma warning disable CS9113 // Parameter is unread.
    IHostApplicationLifetime appLifetime,
#pragma warning restore CS9113 // Parameter is unread.
    ILoggerFactory loggerFactory,
    BackPlaneConfig config)
    : IBasePlaneConnection, IAsyncDisposable
{
    private const string SendAndForgetRequestType = "send-and-forget-request";
    private const string SendAndForgetRequestCancelledType = "send-and-forget-cancelled-request";
    private const string SendRequestType = "send-request";
    private const string SendRequestCancelledType = "send-request-cancelled";
    private const string SendRequestAckType = "send-ack";
    private const string InvokeRequestType = "invoke-request";
    private const string InvokeRequestCancelledType = "invoke-request-cancelled";
    private const string InvokeRequestAckType = "invoke-ack";
    private const string InvokeRequestDoneType = "invoke-done";
    private const string StreamingRequestType = "streaming-request";
    private const string StreamingResponseType = "streaming-response";

    public BackPlaneConfig Config { get; } = config;
    private readonly ILogger<RabbitMQBasePlaneConnection> Logger = loggerFactory.CreateLogger<RabbitMQBasePlaneConnection>();

    private readonly ConcurrentDictionary<RequestArgumentIndexStreamDto, Action<PlaneStreamingResponseDto>> StreamingResponseHandlers = [];
    private readonly ConcurrentDictionary<RequestArgumentIndexDto, IPlaneAsyncEnumerableRegistration> StreamingRequestHandlers = [];
    private readonly ConcurrentDictionary<ServiceId, ServiceConsumerRegistration> ServiceConsumers = new();
    private readonly ConcurrentDictionary<RequestId, CancellationTokenSourceDispose> _activeRequests = new();


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
    public PlaneConnectionId PlaneConnectionId { get; private set; } = PlaneConnectionId.New();

    public async Task TryConnectAsync(CancellationToken ct)
    {
        if (Initialized) return;

        var factory = new ConnectionFactory { Uri = new Uri(Config.ConnectionString) };
        _connection = await factory.CreateConnectionAsync(ct);
        _channel = await _connection.CreateChannelAsync(cancellationToken: ct);
        _publishChannel = await _connection.CreateChannelAsync(cancellationToken: ct);

        var result = await _channel.QueueDeclareAsync(
            queue: $"q.AutoPlane.streaming.{PlaneConnectionId}",
            durable: false, exclusive: true, autoDelete: true, cancellationToken: ct);

        _streamingQueueName = result.QueueName;
        await StartStreamingConsumerAsync(ct);
        Initialized = true;
    }

    public async Task SendFireAndForgetAsync(PlaneRoutingDto routing, byte[] data, CancellationToken ct)
    {
        await TryConnectAsync(ct);

        var sendrequest = new PlaneSendRequestDto(routing, data);
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
            PlaneToChoice.ToAll
            ? $"ex.{routing.ServiceId}.fanout"
            : $"ex.{routing.ServiceId}.direct";

        var routingKey = routing.To switch
        {
            PlaneToChoice.ToAll => "",
            PlaneToChoice.ToPlaneConnectionId => $"connection.{routing.ToPlaneConnectionId}",
            _ => routing.ServiceId.ToString()
        };
        await PublishRawAsync(exchange, routingKey, payload, ct, null, properties);

        //ct.Register(async () =>
        //{
        //    try
        //    {
        //        var sendrequestcancelled = new PlaneSendRequestCancelledDto(routing, "Unknown");
        //        var payload = CreateSendRequestCancelledData(sendrequestcancelled);
        //        var properties = new BasicProperties
        //        {
        //            Type = SendAndForgetRequestCancelledType,
        //            DeliveryMode =
        //                routing.deliveryMode == DeliveryMode.Transient
        //                ? DeliveryModes.Transient
        //                : DeliveryModes.Persistent
        //        };
        //        await PublishRawAsync(exchange, routingKey, payload, ct, null, properties);
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.LogError(ex, "Failed to send cancellation message");
        //    }
        //});
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
        var replyConsumerChannel = await _connection!.CreateChannelAsync(cancellationToken: ct);

        try
        {
            var replyQueue = await replyConsumerChannel.QueueDeclareAsync(
                queue: "", durable: false, exclusive: true, autoDelete: true, cancellationToken: ct);

            var ackCompletion = new TaskCompletionSource<PlaneSendRequestDoneDto>(TaskCreationOptions.RunContinuationsAsynchronously);

            var consumer = new AsyncEventingBasicConsumer(replyConsumerChannel);
            consumer.ReceivedAsync += (_, ea) =>
            {
                if (ea.BasicProperties.CorrelationId != correlationId || ea.BasicProperties.Type != SendRequestAckType)
                    return Task.CompletedTask;

                try
                {
                    var offset = 0;
                    var ack = PlaneSendRequestDoneDtoSpanSerializer.ReadPlaneSendRequestDoneDto(ea.Body.Span, ref offset, Config.MaxPackageSize ?? 512 * 1024);
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

            timeoutCts.CancelAfter(routing.Timeout);
            var message = await ackCompletion.Task.WaitAsync(routing.Timeout, ct);

            if (message.ExceptionMessage != null)
                throw new RemoteException(message.ExceptionMessage, message.InnerExceptionMessage);
        }
        finally
        {
            await replyConsumerChannel.CloseAsync();
        }
    }
    public async IAsyncEnumerable<byte[]> InvokeAsync(PlaneRoutingDto routing, byte[] data, CancellationTokenSourceDispose timeoutCts)
    {
        if (routing.To == PlaneToChoice.ToAll)
        {
            throw new NotSupportedException("Streaming or synchronous RPC calls via ToAll are not supported on the Service Plane. Use ToAny instead.");
        }

        var ct = timeoutCts.Token;
        await TryConnectAsync(ct);

        var correlationId = routing.RequestId.ToString();
        var replyConsumerChannel = await _connection!.CreateChannelAsync(cancellationToken: ct);

        try
        {
            var replyQueue = await replyConsumerChannel.QueueDeclareAsync(
                queue: "", durable: false, exclusive: true, autoDelete: true, cancellationToken: ct);

            var ackCompletion = new TaskCompletionSource<PlaneInvokeRequestAckDto>(TaskCreationOptions.RunContinuationsAsynchronously);

            var consumer = new AsyncEventingBasicConsumer(replyConsumerChannel);
            consumer.ReceivedAsync += (_, ea) =>
            {
                timeoutCts.CancelAfter(routing.Timeout);

                if (ea.BasicProperties.CorrelationId != correlationId)
                    return Task.CompletedTask;

                if (ea.BasicProperties.Type == InvokeRequestAckType)
                {
                    try
                    {
                        var offset = 0;
                        var ack = PlaneInvokeRequestAckDtoSpanSerializer.ReadPlaneInvokeRequestAckDto(ea.Body.Span, ref offset, Config.MaxPackageSize ?? 512 * 1024);
                        if (ack != null) ackCompletion.TrySetResult(ack);
                    }
                    catch (Exception ex)
                    {
                        ackCompletion.TrySetException(ex);
                    }
                }
                if (ea.BasicProperties.Type == InvokeRequestDoneType)
                {
                    try
                    {
                        var offset = 0;
                        var message = PlaneInvokeRequestDoneDtoSpanSerializer.ReadPlaneInvokeRequestDoneDto(ea.Body.Span, ref offset, Config.MaxPackageSize ?? 512 * 1024);
                        if (message != null)
                        {
                            if (message.Cancelled)
                                ackCompletion.SetCanceled(ct);
                            else if (message.ExceptionMessage != null)
                                ackCompletion.SetException(new RemoteException(message.ExceptionMessage, message.InnerExceptionMessage));
                            else
                                ackCompletion.SetCanceled(ct);
                        }
                    }
                    catch (Exception ex)
                    {
                        ackCompletion.TrySetException(ex);
                    }
                }
                return Task.CompletedTask;
            };

            await replyConsumerChannel.BasicConsumeAsync(replyQueue.QueueName, autoAck: true, consumer: consumer, cancellationToken: ct);

            // STAP 1 & 2: => Invoke sturen & wachten op de InvokeAck / InvokeDone
            await PublishAsync(routing, data, isInvoke: true, ct, replyTo: replyQueue.QueueName, correlationId: correlationId);

            timeoutCts.CancelAfter(routing.Timeout);
            var ackResult = await ackCompletion.Task.WaitAsync(routing.Timeout, ct);

            var streamId = StreamId.New();
            var argumentIndex = -1; // -1 representeert de return-stream van de methode
            var responseKey = new RequestArgumentIndexStreamDto(ackResult.Routing.RequestId, argumentIndex, streamId);

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var responseCompletion = new TaskCompletionSource<PlaneStreamingResponseDto>(TaskCreationOptions.RunContinuationsAsynchronously);

                    StreamingResponseHandlers[responseKey] = response => responseCompletion.TrySetResult(response);

                    // STAP 3 & 5: => Vraag om volgend item / "Is er nog meer?" via StreamingRequest
                    await SendStreamingRequestAsync(ackResult.Routing, argumentIndex, streamId, cancelled: false, ct);

                    // STAP 4 & 6: <= Wacht op de StreamingResponse van de service
                    timeoutCts.CancelAfter(routing.Timeout);
                    var message = await responseCompletion.Task.WaitAsync(routing.Timeout, ct);

                    if (message.ExceptionMessage != null)
                        throw new RemoteException(message.ExceptionMessage, message.InnerExceptionMessage);

                    // STAP 6b: De service stuurt Done terug, we sluiten de lus
                    if (message.IsCompleted)
                    {
                        break;
                    }

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
            await replyConsumerChannel.CloseAsync();
        }
    }

    private Task SendStreamingRequestAsync(PlaneRoutingDto routing, int argumentIndex, StreamId streamId, bool cancelled, CancellationToken ct)
    {
        if (!Initialized || _channel == null || _publishChannel == null || _connection == null)
            throw new InvalidOperationException("Plane connection is not initialized. Call TryConnectAsync first.");

        var request = new PlaneStreamingRequestDto(routing, argumentIndex, streamId, cancelled);
        var properties = new BasicProperties { Type = StreamingRequestType, ReplyTo = _streamingQueueName };
        byte[] data = CreateStreamingRequestData(request);

        return PublishRawAsync("", $"q.AutoPlane.streaming.{routing.FromPlaneConnectionId}", data, ct, properties: properties);
    }
    private async Task PublishAsync(PlaneRoutingDto routing, byte[] data, bool isInvoke, CancellationToken ct, string? replyTo = null, string? correlationId = null)
    {
        if (!Initialized || _channel == null || _publishChannel == null || _connection == null)
            throw new InvalidOperationException("Plane connection is not initialized. Call TryConnectAsync first.");

        byte[] payload;
        if (isInvoke)
        {
            var invokerequest = new PlaneInvokeRequestDto(routing, data);
            payload = CreateInvokeRequestData(invokerequest);
        }
        else
        {
            var sendrequest = new PlaneSendRequestDto(routing, data);
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
            PlaneToChoice.ToAll
            ? $"ex.{routing.ServiceId}.fanout"
            : $"ex.{routing.ServiceId}.direct";

        var routingKey = routing.To switch
        {
            PlaneToChoice.ToAll => "",
            PlaneToChoice.ToPlaneConnectionId => $"connection.{routing.ToPlaneConnectionId}",
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
                    var invokerequest = new PlaneInvokeRequestCancelledDto(routing, "Unknown");
                    payload = CreateInvokeRequestCancelledData(invokerequest);
                }
                else
                {
                    var sendrequest = new PlaneSendRequestCancelledDto(routing, "Unknown");
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

    // This is called AFTER the service has been registered for service routing
    // (with the Send_SendRequest_ToServiceAsync / Send_InvokeRequest_ToServiceAsync calls)
    protected async Task SendSubscribeAsync(object service, ServiceId serviceId, CancellationToken ct)
    {
        if (!Initialized || _channel == null || _publishChannel == null || _connection == null)
            throw new InvalidOperationException("Plane verbinding is niet geïnitialiseerd. Roep eerst TryConnectAsync aan.");

        await SubscriptionLock.WaitAsync(ct);
        try
        {
            if (ServiceConsumers.TryGetValue(serviceId, out var existing)) { existing.SubscriptionCount++; return; }
            var registration = new ServiceConsumerRegistration { SubscriptionCount = 1 };
            var directExchange = $"ex.{serviceId}.direct";
            var fanoutExchange = $"ex.{serviceId}.fanout";
            var anyQueue = $"q.{serviceId}.any";
            var allQueue = $"q.{serviceId}.all.{PlaneConnectionId}";
            var targetedQueue = $"q.{serviceId}.connection.{PlaneConnectionId}";
            await _channel!.ExchangeDeclareAsync(directExchange, ExchangeType.Direct, durable: true, cancellationToken: ct);
            await _channel.ExchangeDeclareAsync(fanoutExchange, ExchangeType.Fanout, durable: true, cancellationToken: ct);
            await _channel.QueueDeclareAsync(anyQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
            await _channel.QueueBindAsync(anyQueue, directExchange, serviceId.ToString(), cancellationToken: ct);
            await _channel.QueueDeclareAsync(allQueue, durable: false, exclusive: true, autoDelete: true, cancellationToken: ct);
            await _channel.QueueDeclareAsync(targetedQueue, durable: false, exclusive: true, autoDelete: true, cancellationToken: ct);
            await _channel.QueueBindAsync(targetedQueue, directExchange, $"connection.{PlaneConnectionId}", cancellationToken: ct);
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
            await StopConsumersAsync(registration, ct);
            if (_channel == null)
                return;
            await _channel.QueueDeleteAsync($"q.{serviceId}.all.{PlaneConnectionId}", cancellationToken: ct);
            await _channel.QueueDeleteAsync($"q.{serviceId}.connection.{PlaneConnectionId}", cancellationToken: ct);
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
                            var sendRequest1 = PlaneSendRequestDtoSpanSerializer.ReadPlaneSendRequestDto(body, ref offset1, Config.MaxPackageSize ?? 512 * 1024);
                            await HandleSendAndForgetRequest(sendRequest1, _lifetimeCts.Token);
                            break;
                        case SendAndForgetRequestCancelledType:
                            var offset2 = 0;
                            var sendRequestCancelled2 = PlaneSendRequestCancelledDtoSpanSerializer.ReadPlaneSendRequestCancelledDto(body, ref offset2, Config.MaxPackageSize ?? 512 * 1024);
                            await HandleSendAndForgetRequestCancelled(sendRequestCancelled2, _lifetimeCts.Token);
                            break;
                        case SendRequestType:
                            var offset3 = 0;
                            var sendRequest3 = PlaneSendRequestDtoSpanSerializer.ReadPlaneSendRequestDto(body, ref offset3, Config.MaxPackageSize ?? 512 * 1024);
                            var replyTo3 = ea.BasicProperties.ReplyTo;
                            var correlationId3 = ea.BasicProperties.CorrelationId;
                            if (string.IsNullOrWhiteSpace(replyTo3) || string.IsNullOrWhiteSpace(correlationId3))
                                throw new InvalidOperationException("Send request is missing ReplyTo or CorrelationId.");
                            await HandleSendRequestAsync(sendRequest3, replyTo3, correlationId3, _lifetimeCts.Token);
                            break;
                        case SendRequestCancelledType:
                            var offset4 = 0;
                            var sendRequestCancelled4 = PlaneSendRequestCancelledDtoSpanSerializer.ReadPlaneSendRequestCancelledDto(body, ref offset4, Config.MaxPackageSize ?? 512 * 1024);
                            await HandleSendRequestCancelled(sendRequestCancelled4, _lifetimeCts.Token);
                            break;
                        case InvokeRequestType:
                            var offset5 = 0;
                            var invRequest5 = PlaneInvokeRequestDtoSpanSerializer.ReadPlaneInvokeRequestDto(body, ref offset5, Config.MaxPackageSize ?? 512 * 1024);
                            var replyTo5 = ea.BasicProperties.ReplyTo;
                            var correlationId5 = ea.BasicProperties.CorrelationId;
                            if (string.IsNullOrWhiteSpace(replyTo5) || string.IsNullOrWhiteSpace(correlationId5))
                                throw new InvalidOperationException("Invoke request is missing ReplyTo or CorrelationId.");
                            await HandleInvokeRequestAsync(invRequest5, replyTo5, correlationId5, _lifetimeCts.Token);
                            break;
                        case InvokeRequestCancelledType:
                            var offset6 = 0;
                            var invokeRequestCancelled6 = PlaneInvokeRequestCancelledDtoSpanSerializer.ReadPlaneInvokeRequestCancelledDto(body, ref offset6, Config.MaxPackageSize ?? 512 * 1024);
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

    private async Task HandleSendAndForgetRequest(PlaneSendRequestDto request, CancellationToken ct)
    {
        using var timeoutCts = new CancellationTokenSourceDispose(request.Routing.Timeout, ct);
        _activeRequests[request.Routing.RequestId] = timeoutCts;
        timeoutCts.CancelAfter(request.Routing.Timeout);
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
    private async Task HandleSendAndForgetRequestCancelled(PlaneSendRequestCancelledDto request, CancellationToken token)
    {
        if (_activeRequests.TryGetValue(request.Routing.RequestId, out var cts) == false)
            return;

        await cts.CancelAsync();
    }
    private async Task HandleSendRequestAsync(PlaneSendRequestDto request, string replyTo, string correlationId, CancellationToken ct)
    {
        using var timeoutCts = new CancellationTokenSourceDispose(request.Routing.Timeout, ct);
        _activeRequests[request.Routing.RequestId] = timeoutCts;
        timeoutCts.CancelAfter(request.Routing.Timeout);
        ct = timeoutCts.Token;

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
            var properties = new BasicProperties { Type = SendRequestAckType, CorrelationId = correlationId };
            var ackData = CreateSendRequestAckData(ack);
            await PublishRawAsync("", replyTo, ackData, timeoutCts.Token, correlationId, properties);
        }
        finally
        {
            _activeRequests.TryRemove(request.Routing.RequestId, out _);
        }
    }
    private async Task HandleSendRequestCancelled(PlaneSendRequestCancelledDto request, CancellationToken token)
    {
        if (_activeRequests.TryGetValue(request.Routing.RequestId, out var cts) == false)
            return;

        await cts.CancelAsync();
    }
    private async Task HandleInvokeRequestAsync(PlaneInvokeRequestDto request, string replyTo, string correlationId, CancellationToken ct)
    {
        var timeoutCts = new CancellationTokenSourceDispose(request.Routing.Timeout, ct);
        _activeRequests[request.Routing.RequestId] = timeoutCts;
        timeoutCts.CancelAfter(request.Routing.Timeout);
        timeoutCts.Token.Register(() =>
        {
            Task.Run(async () =>
            {
                await Task.Delay(50);
                _activeRequests.TryRemove(request.Routing.RequestId, out _);
                timeoutCts.Dispose();
            });
        });
        ct = timeoutCts.Token;

        PlaneInvokeRequestAckDto ack;
        try
        {
            var responseRouting = request.Routing with { FromPlaneConnectionId = PlaneConnectionId, To = PlaneToChoice.ToPlaneConnectionId, ToPlaneConnectionId = request.Routing.FromPlaneConnectionId };
            var enumerable = Send_InvokeRequest_ToServiceAsync(request, timeoutCts.Token);
            RegisterAsyncEnumerableArgument<byte[]>(request.Routing, -1, enumerable, static item => item, timeoutCts);
            ack = new PlaneInvokeRequestAckDto(responseRouting, false, null, null);
            var properties = new BasicProperties { Type = InvokeRequestAckType, CorrelationId = correlationId };
            byte[] ackData = CreateInvokeRequestAckData(ack);
            await PublishRawAsync("", replyTo, ackData, timeoutCts.Token, correlationId, properties);
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
            var properties = new BasicProperties { Type = InvokeRequestDoneType, CorrelationId = correlationId };
            byte[] ackData = CreateInvokeRequestDoneData(done);
            await PublishRawAsync("", replyTo, ackData, timeoutCts.Token, correlationId, properties);
        }

    }
    private async Task HandleInvokeRequestCancelled(PlaneInvokeRequestCancelledDto request, CancellationToken token)
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
                                    ExceptionMessage: request.Cancelled ? null : "De streamingregistratie bestaat niet meer.",
                                    InnerExceptionMessage: null,
                                    BinaryData: []);
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
                            var streamingResp = PlaneStreamingResponseDtoSpanSerializer.ReadPlaneStreamingResponseDto(body, ref offset2, Config.MaxPackageSize ?? 512 * 1024);
                            await ReceiveStreamingResponseAsync(streamingResp, _lifetimeCts.Token);
                            break;
                        default:
                            throw new InvalidOperationException($"Unknown streaming message type: {ea.BasicProperties.Type ?? "(null)"}");
                    }
                    await consumerChannel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                }
                catch
                {
                    await consumerChannel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
                }
            };
            _streamingConsumerTag = await consumerChannel.BasicConsumeAsync(_streamingQueueName!, autoAck: false, consumer: consumer, cancellationToken: ct);
            _streamingConsumerChannel = consumerChannel;
        }
        catch { await consumerChannel.CloseAsync(); throw; }
    }

    private Task ReceiveStreamingResponseAsync(PlaneStreamingResponseDto response, CancellationToken ct)
    {
        var key = new RequestArgumentIndexStreamDto(response.Routing.RequestId, response.ArgumentIndex, response.StreamId);
        if (StreamingResponseHandlers.TryGetValue(key, out var handler)) handler(response);
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
        StreamingRequestHandlers.TryAdd(
            new RequestArgumentIndexDto(routing.RequestId, argumentIndex),
            new PlaneAsyncEnumerableRegistration<T>(
                async (activeStreams, streamId, cancelled, ct) =>
                {
                    var (enumerator, gate, linkedCts) = activeStreams.GetOrAdd(streamId, _ =>
                    {
                        var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, cts.Token);
                        return new PlaneAsyncEnumerableRegistrationInstance<T>(source.GetAsyncEnumerator(linked.Token), new SemaphoreSlim(1, 1), linked);
                    });

                    async Task Cleanup() { if (activeStreams.TryRemove(streamId, out _)) { await enumerator.DisposeAsync(); linkedCts.Dispose(); } }
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
                            localInnerExceptionMessage = ex.InnerException?.Message;
                        }

                        // Als er een exception was, markeren we de dto direct als Completed met de ExceptionMessage
                        var isCompleted = !hasNext || cancelled || localExceptionMessage != null;

                        var response = new PlaneStreamingResponseDto(
                            routing,
                            argumentIndex,
                            streamId,
                            IsCompleted: isCompleted,
                            IsCancelled: cancelled,
                            ExceptionMessage: localExceptionMessage,
                            InnerExceptionMessage: localInnerExceptionMessage,
                            BinaryData: hasNext ? serializer(currentItem) : []
                        );

                        if (isCompleted)
                            await Cleanup();

                        return response;
                    }
                    catch (OperationCanceledException ex)
                    {
                        await Cleanup();
                        return new PlaneStreamingResponseDto(
                            routing, argumentIndex, streamId, true, true, ex.Message, ex.InnerException?.Message, []);
                    }
                    catch (Exception ex)
                    {
                        await Cleanup();
                        return new PlaneStreamingResponseDto(
                            routing, argumentIndex, streamId, true, false, ex.Message, ex.InnerException?.Message, []);
                    }
                    finally
                    {
                        if (entered)
                            gate.Release();
                    }
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

    protected abstract Task Send_SendRequest_ToServiceAsync(PlaneSendRequestDto sendRequest, CancellationToken ct);
    protected abstract IAsyncEnumerable<byte[]> Send_InvokeRequest_ToServiceAsync(PlaneInvokeRequestDto invokeRequest, CancellationToken ct);

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