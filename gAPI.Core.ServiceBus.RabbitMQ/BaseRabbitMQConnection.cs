using gAPI.Core.Dtos;
using gAPI.Core.Enums;
using gAPI.Core.Helpers;
using gAPI.Core.Ids;
using gAPI.Core.Interfaces;
using gAPI.Core.ServiceBus.Config;
using gAPI.Core.ServiceBus.Helpers;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Concurrent;

namespace gAPI.Core.ServiceBus.RabbitMQ;

public abstract class BaseRabbitMQConnection(
    ServiceBusConfig busConfig)
    : IBaseBusConnection, IAsyncDisposable
{
    private const string SendAndForgetRequestType = "send-and-forget-request";
    private const string SendRequestType = "send-request";
    private const string SendAckType = "send-ack";
    private const string InvokeRequestType = "invoke-request";
    private const string InvokeAckType = "invoke-ack";
    private const string StreamingRequestType = "streaming-request";
    private const string StreamingResponseType = "streaming-response";

    private readonly ConcurrentDictionary<RequestArgumentIndexStreamDto, Action<BusStreamingResponseDto>> StreamingResponseHandlers = [];
    private readonly ConcurrentDictionary<RequestArgumentIndexDto, IBusAsyncEnumerableRegistration> StreamingRequestHandlers = [];
    private readonly ConcurrentDictionary<ServiceId, ServiceConsumerRegistration> ServiceConsumers = new();

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

    /// <summary>
    /// PATROON 1: Send Fire and Forget. 
    /// Verstuurt het bericht en wacht puur tot het fysiek op de RabbitMQ-bus staat.
    /// </summary>
    public async Task SendFireAndForgetAsync(BusRoutingDto routing, byte[] data, CancellationTokenSource timeoutCts)
    {
        var ct = timeoutCts.Token;
        await TryConnectAsync(ct);

        var sendrequest = new BusSendRequestDto(routing, data);
        var offset = 0;
        var length = BusSendRequestDtoSpanSerializer.Length(ref offset, sendrequest);
        var payload = new byte[length];
        var span = new Span<byte>(payload);
        offset = 0;
        BusSendRequestDtoSpanSerializer.Write(ref span, ref offset, sendrequest);

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
    }

    /// <summary>
    /// PATROON 2: Send and Wait for Application ACK.
    /// Verstuurt het verzoek en wacht tot de ontvangende service de taak succesvol heeft afgerond (of gefaald).
    /// </summary>
    public async Task SendAsync(BusRoutingDto routing, byte[] data, CancellationTokenSource timeoutCts)
    {
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
                if (ea.BasicProperties.CorrelationId != correlationId || ea.BasicProperties.Type != SendAckType)
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

    /// <summary>
    /// PATROON 3: Streaming Invoke (IAsyncEnumerable).
    /// Regelt de stapsgewijze heen-en-weer (ping-pong) communicatie per streaming item.
    /// </summary>
    public async IAsyncEnumerable<byte[]> InvokeAsync(BusRoutingDto routing, byte[] data, CancellationTokenSource timeoutCts)
    {
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

                if (ea.BasicProperties.CorrelationId != correlationId || ea.BasicProperties.Type != InvokeAckType)
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

    private async Task PublishAsync(BusRoutingDto routing, byte[] data, bool isInvoke, CancellationToken ct, string? replyTo = null, string? correlationId = null)
    {
        if (!Initialized || _channel == null || _publishChannel == null || _connection == null)
            throw new InvalidOperationException("Bus verbinding is niet geïnitialiseerd. Roep eerst TryConnectAsync aan.");

        byte[] payload;
        if (isInvoke)
        {
            var invokerequest = new BusInvokeRequestDto(routing, data);

            var offset = 0;
            var length = BusInvokeRequestDtoSpanSerializer.Length(ref offset, invokerequest);
            payload = new byte[length];

            offset = 0;
            var span = new Span<byte>(payload);
            BusInvokeRequestDtoSpanSerializer.Write(ref span, ref offset, invokerequest);
        }
        else
        {
            var sendrequest = new BusSendRequestDto(routing, data);

            var offset = 0;
            var length = BusSendRequestDtoSpanSerializer.Length(ref offset, sendrequest);
            payload = new byte[length];

            offset = 0;
            var span = new Span<byte>(payload);
            BusSendRequestDtoSpanSerializer.Write(ref span, ref offset, sendrequest);
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

    private Task SendStreamingRequestAsync(BusRoutingDto routing, int argumentIndex, StreamId streamId, bool cancelled, CancellationToken ct)
    {
        if (!Initialized || _channel == null || _publishChannel == null || _connection == null)
            throw new InvalidOperationException("Bus verbinding is niet geïnitialiseerd. Roep eerst TryConnectAsync aan.");

        var request = new BusStreamingRequestDto(routing, argumentIndex, streamId, cancelled);
        var properties = new BasicProperties { Type = StreamingRequestType, ReplyTo = _streamingQueueName };

        var offset = 0;
        var length = BusStreamingRequestDtoSpanSerializer.Length(ref offset, request);
        var data = new byte[length];

        offset = 0;
        var span = new Span<byte>(data);
        BusStreamingRequestDtoSpanSerializer.Write(ref span, ref offset, request);

        return PublishRawAsync("", $"q.autobus.streaming.{routing.FromBusConnectionId}", data, ct, properties: properties);
    }
    private Task ReceiveStreamingResponseAsync(BusStreamingResponseDto response, CancellationToken ct)
    {
        var key = new RequestArgumentIndexStreamDto(response.Routing.RequestId, response.ArgumentIndex, response.StreamId);
        if (StreamingResponseHandlers.TryGetValue(key, out var handler)) handler(response);
        return Task.CompletedTask;
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
    private async Task StopConsumersAsync(ServiceConsumerRegistration registration, CancellationToken ct)
    {
        foreach (var consumer in registration.Consumers)
        {
            try { await consumer.Channel.BasicCancelAsync(consumer.ConsumerTag, cancellationToken: ct); }
            finally { await consumer.Channel.CloseAsync(); }
        }
        registration.Consumers.Clear();
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
                            var sendRequest = BusSendRequestDtoSpanSerializer.ReadBusSendRequestDto(body, ref offset1);
                            await Send_SendRequest_ToServiceAsync(sendRequest, _lifetimeCts.Token);
                            break;
                        case SendRequestType:
                            var offset2 = 0;
                            var sendRequest2 = BusSendRequestDtoSpanSerializer.ReadBusSendRequestDto(body, ref offset2);
                            var replyTo2 = ea.BasicProperties.ReplyTo;
                            var correlationId2 = ea.BasicProperties.CorrelationId;
                            if (string.IsNullOrWhiteSpace(replyTo2) || string.IsNullOrWhiteSpace(correlationId2)) throw new InvalidOperationException("Send-verzoek mist ReplyTo of CorrelationId.");
                            await HandleSendRequestAsync(sendRequest2, replyTo2, correlationId2, _lifetimeCts);
                            break;
                        case InvokeRequestType:
                            var offset3 = 0;
                            var invRequest = BusInvokeRequestDtoSpanSerializer.ReadBusInvokeRequestDto(body, ref offset3);
                            var replyTo3 = ea.BasicProperties.ReplyTo;
                            var correlationId3 = ea.BasicProperties.CorrelationId;
                            if (string.IsNullOrWhiteSpace(replyTo3) || string.IsNullOrWhiteSpace(correlationId3)) throw new InvalidOperationException("Invoke-verzoek mist ReplyTo of CorrelationId.");
                            await HandleInvokeRequestAsync(invRequest, replyTo3, correlationId3, _lifetimeCts);
                            break;
                        default:
                            throw new InvalidOperationException($"Onbekend busberichttype: {messageType ?? "(null)"}");
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
    private async Task HandleSendRequestAsync(BusSendRequestDto request, string replyTo, string correlationId, CancellationTokenSource cts)
    {
        BusSendRequestAckDto ack;
        try
        {
            var responseRouting = request.Routing with { FromBusConnectionId = BusConnectionId, To = BusToChoice.ToBusConnectionId, ToBusConnectionId = request.Routing.FromBusConnectionId };
            await Send_SendRequest_ToServiceAsync(request, cts.Token);
            ack = new BusSendRequestAckDto(responseRouting, null);
        }
        catch (Exception ex)
        {
            ack = new BusSendRequestAckDto(request.Routing with { FromBusConnectionId = BusConnectionId, To = BusToChoice.ToBusConnectionId, ToBusConnectionId = request.Routing.FromBusConnectionId }, ex.Message);
        }
        var properties = new BasicProperties { Type = SendAckType, CorrelationId = correlationId };


        var offset = 0;
        var length = BusSendRequestAckDtoSpanSerializer.Length(ref offset, ack);
        var data = new byte[length];

        offset = 0;
        var span = new Span<byte>(data);
        BusSendRequestAckDtoSpanSerializer.Write(ref span, ref offset, ack);

        await PublishRawAsync("", replyTo, data, cts.Token, correlationId, properties);
    }
    private async Task HandleInvokeRequestAsync(BusInvokeRequestDto request, string replyTo, string correlationId, CancellationTokenSource cts)
    {
        BusInvokeRequestAckDto ack;
        try
        {
            var responseRouting = request.Routing with { FromBusConnectionId = BusConnectionId, To = BusToChoice.ToBusConnectionId, ToBusConnectionId = request.Routing.FromBusConnectionId };
            var enumerable = Send_InvokeRequest_ToServiceAsync(request, cts.Token);
            RegisterAsyncEnumerableArgument<byte[]>(request.Routing, -1, enumerable, static item => item, cts);
            ack = new BusInvokeRequestAckDto(responseRouting, null);
        }
        catch (Exception ex)
        {
            ack = new BusInvokeRequestAckDto(request.Routing with { FromBusConnectionId = BusConnectionId, To = BusToChoice.ToBusConnectionId, ToBusConnectionId = request.Routing.FromBusConnectionId }, ex.Message);
        }
        var properties = new BasicProperties { Type = InvokeAckType, CorrelationId = correlationId };

        var offset = 0;
        var length = BusInvokeRequestAckDtoSpanSerializer.Length(ref offset, ack);
        var data = new byte[length];

        offset = 0;
        var span = new Span<byte>(data);
        BusInvokeRequestAckDtoSpanSerializer.Write(ref span, ref offset, ack);

        await PublishRawAsync("", replyTo, data, cts.Token, correlationId, properties);
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

                                var offset = 0;
                                var length = BusStreamingResponseDtoSpanSerializer.Length(ref offset, response);
                                var data = new byte[length];
                                offset = 0;
                                var span = new Span<byte>(data);
                                BusStreamingResponseDtoSpanSerializer.Write(ref span, ref offset, response);

                                await PublishRawAsync("", ea.BasicProperties.ReplyTo, data, _lifetimeCts.Token, properties: properties);
                            }
                            if (request.ArgumentIndex == -1 && (response.IsCompleted || response.IsCancelled || response.ExceptionMessage != null))
                            {
                                await UnRegisterAsyncEnumerableArgument(request.Routing, -1);
                            }
                            break;
                        case StreamingResponseType:
                            var offset2 = 0;
                            var streamingResp = BusStreamingResponseDtoSpanSerializer.ReadBusStreamingResponseDto(body, ref offset2);
                            await ReceiveStreamingResponseAsync(streamingResp, _lifetimeCts.Token);
                            break;
                        default:
                            throw new InvalidOperationException($"Onbekend streamingberichttype: {ea.BasicProperties.Type ?? "(null)"}");
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

    protected IAsyncEnumerable<T> RegisterRemoteAsyncEnumerableArgument<T>(BusRoutingDto routing, int argumentIndex, Func<byte[], T> deserializer)
    {
        return new RemoteAsyncEnumerable<T>(
            construct: (StreamId streamId, IRemoteAsyncEnumerator<T> enumerator, CancellationToken ct) =>
            {
                var key = new RequestArgumentIndexStreamDto(routing.RequestId, argumentIndex, streamId);
                StreamingResponseHandlers.TryAdd(key, response =>
                {
                    if (response.IsCancelled)
                    {
                        enumerator.Complete(new TaskCanceledException(response.ExceptionMessage));
                        return;
                    }

                    if (response.ExceptionMessage != null)
                    {
                        enumerator.Complete(new RemoteException(response.ExceptionMessage));
                        return;
                    }

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

    public void RegisterAsyncEnumerableArgument<T>(BusRoutingDto routing, int argumentIndex, IAsyncEnumerable<T> source, Func<T, byte[]> serializer, CancellationTokenSource ct)
    {
        StreamingRequestHandlers.TryAdd(
            new RequestArgumentIndexDto(routing.RequestId, argumentIndex), 
            new BusAsyncEnumerableRegistration<T>(
                async (activeStreams, streamId, cancelled, ct) =>
        {
            var (enumerator, gate, linkedCts) = activeStreams.GetOrAdd(streamId, _ =>
            {
                var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, ct);
                return new BusAsyncEnumerableRegistrationInstance<T>(source.GetAsyncEnumerator(linked.Token), new SemaphoreSlim(1, 1), linked);
            });
            async Task Cleanup() { if (activeStreams.TryRemove(streamId, out _)) { await enumerator.DisposeAsync(); linkedCts.Dispose(); } }
            var entered = false;
            try
            {
                await gate.WaitAsync(ct); entered = true;
                var hasNext = !cancelled && await enumerator.MoveNextAsync();
                var response = new BusStreamingResponseDto(routing, argumentIndex, streamId, !hasNext, cancelled, null, hasNext ? serializer(enumerator.Current) : []);
                if (!hasNext || cancelled) await Cleanup();
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