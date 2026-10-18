
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
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;

namespace gAPI.Core.ServiceBus.RabbitMQ;

public abstract class BaseRabbitMQConnection(
    DeliveryModes deliveryMode,
    ServiceBusConfig busConfig)
    : IBaseBusConnection, IAsyncDisposable
{
    private const string InvokeRequestType = "invoke-request";
    private const string InvokeAckType = "invoke-ack";
    private const string SendRequestType = "send-request";
    private const string StreamingRequestType = "streaming-request";
    private const string StreamingResponseType = "streaming-response";

    private readonly ConcurrentDictionary<
        RequestArgumentIndexStreamDto,
        Action<BusStreamingResponseDto>> StreamingResponseHandlers = [];

    private readonly ConcurrentDictionary<
        RequestArgumentIndexDto,
        IAsyncEnumerableRegistration> StreamingRequestHandlers = [];

    private readonly ConcurrentDictionary<ServiceId, ServiceConsumerRegistration>
        ServiceConsumers = new();

    private readonly SemaphoreSlim SubscriptionLock = new(1, 1);
    private readonly SemaphoreSlim PublishLock = new(1, 1);

    private readonly CancellationTokenSource _lifetimeCts = new();

    private IConnection? _connection;
    private IChannel? _channel;
    private IChannel? _publishChannel;
    private IChannel? _streamingConsumerChannel;

    private string? _streamingQueueName;
    private string? _streamingConsumerTag;

    private static readonly byte[] EndOfStreamMarker = [];

    public bool Initialized { get; private set; }

    public BusConnectionId BusConnectionId { get; private set; }
        = BusConnectionId.New();

    public async Task TryConnectAsync(CancellationToken ct)
    {
        if (Initialized)
            return;

        var factory = new ConnectionFactory
        {
            // Geef de volledige connection string mee via de Uri property. 
            // RabbitMQ splitst dit zelf intern naar Host, User, Password, etc.
            Uri = new Uri(busConfig.ConnectionString)
        };

        _connection = await factory.CreateConnectionAsync(ct);
        _channel = await _connection.CreateChannelAsync(
            cancellationToken: ct);
        _publishChannel = await _connection.CreateChannelAsync(
            cancellationToken: ct);

        var result = await _channel.QueueDeclareAsync(
            queue: $"q.autobus.streaming.{BusConnectionId}",
            durable: false,
            exclusive: true,
            autoDelete: true,
            cancellationToken: ct);

        _streamingQueueName = result.QueueName;

        await StartStreamingConsumerAsync(ct);

        Initialized = true;
    }

    protected async Task SendSubscribeAsync(object service, ServiceId serviceId, CancellationToken ct)
    {
        EnsureInitialized();

        await SubscriptionLock.WaitAsync(ct);

        try
        {
            if (ServiceConsumers.TryGetValue(serviceId, out var existing))
            {
                existing.SubscriptionCount++;
                return;
            }

            var registration = new ServiceConsumerRegistration
            {
                SubscriptionCount = 1
            };

            var directExchange = $"ex.{serviceId}.direct";
            var fanoutExchange = $"ex.{serviceId}.fanout";

            var anyQueue = $"q.{serviceId}.any";
            var allQueue = $"q.{serviceId}.all.{BusConnectionId}";
            var targetedQueue = $"q.{serviceId}.connection.{BusConnectionId}";

            await _channel!.ExchangeDeclareAsync(
                directExchange,
                ExchangeType.Direct,
                durable: true,
                cancellationToken: ct);

            await _channel.ExchangeDeclareAsync(
                fanoutExchange,
                ExchangeType.Fanout,
                durable: true,
                cancellationToken: ct);

            // ToAny: gedeelde queue, met competing consumers.
            await _channel.QueueDeclareAsync(
                anyQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: ct);

            await _channel.QueueBindAsync(
                anyQueue,
                directExchange,
                serviceId.ToString(),
                cancellationToken: ct);

            // ToAll: aparte queue per busverbinding.
            await _channel.QueueDeclareAsync(
                allQueue,
                durable: false,
                exclusive: true, // <-- Aangepast naar true
                autoDelete: true,
                cancellationToken: ct);

            // Gerichte routering naar deze busverbinding.
            await _channel.QueueDeclareAsync(
                targetedQueue,
                durable: false,
                exclusive: true, // <-- Aangepast naar true
                autoDelete: true,
                cancellationToken: ct);

            await _channel.QueueBindAsync(
                targetedQueue,
                directExchange,
                $"connection.{BusConnectionId}",
                cancellationToken: ct);

            try
            {
                registration.Consumers.Add(
                    await StartConsumerAsync(anyQueue, ct));

                registration.Consumers.Add(
                    await StartConsumerAsync(allQueue, ct));

                registration.Consumers.Add(
                    await StartConsumerAsync(targetedQueue, ct));

                ServiceConsumers.TryAdd(serviceId, registration);
            }
            catch
            {
                await StopConsumersAsync(
                    registration,
                    CancellationToken.None);

                await _channel.QueueDeleteAsync(
                    allQueue,
                    cancellationToken: CancellationToken.None);

                await _channel.QueueDeleteAsync(
                    targetedQueue,
                    cancellationToken: CancellationToken.None);

                // De gedeelde ToAny-queue blijft bestaan.
                throw;
            }
        }
        finally
        {
            SubscriptionLock.Release();
        }
    }
    protected async Task SendUnsubscribeAsync(object service, ServiceId serviceId, CancellationToken ct)
    {
        await SubscriptionLock.WaitAsync(ct);

        try
        {
            if (!ServiceConsumers.TryGetValue(
                serviceId, out var registration))
            {
                return;
            }

            if (--registration.SubscriptionCount > 0)
                return;

            ServiceConsumers.TryRemove(serviceId, out _);

            await StopConsumersAsync(registration, ct);

            // Alleen queues die bij deze busverbinding horen verwijderen.
            await _channel!.QueueDeleteAsync(
                $"q.{serviceId}.all.{BusConnectionId}",
                cancellationToken: ct);

            await _channel.QueueDeleteAsync(
                $"q.{serviceId}.connection.{BusConnectionId}",
                cancellationToken: ct);

            // q.{serviceId}.any is gedeeld en blijft bestaan.
        }
        finally
        {
            SubscriptionLock.Release();
        }
    }

    public async Task SendFireAndForgetAsync(BusRoutingDto routing, byte[] data, CancellationToken ct)
    {
        await TryConnectAsync(ct);
        await PublishAsync(routing, data, true, false, ct);
    }
    public async Task SendAsync(BusRoutingDto routing, byte[] data, CancellationToken ct)
    {
        await TryConnectAsync(ct);
        await PublishAsync(routing, data, false, false, ct);
    }
    public async IAsyncEnumerable<byte[]> InvokeAsync(BusRoutingDto routing, byte[] data, [EnumeratorCancellation] CancellationToken ct)
    {
        await TryConnectAsync(ct);
        //EnsureInitialized();

        var correlationId = routing.RequestId.ToString();

        var replyConsumerChannel = await _connection!.CreateChannelAsync(
            cancellationToken: ct);

        try
        {
            var replyQueue = await replyConsumerChannel.QueueDeclareAsync(
                queue: "",
                durable: false,
                exclusive: true,
                autoDelete: true,
                cancellationToken: ct);

            var ackCompletion =
                new TaskCompletionSource<BusInvokeRequestAckDto>(
                    TaskCreationOptions.RunContinuationsAsynchronously);

            var consumer = new AsyncEventingBasicConsumer(
                replyConsumerChannel);

            consumer.ReceivedAsync += (_, ea) =>
            {
                if (ea.BasicProperties.CorrelationId != correlationId ||
                    ea.BasicProperties.Type != InvokeAckType)
                {
                    return Task.CompletedTask;
                }

                try
                {
                    var ack =
                        JsonSerializer.Deserialize<BusInvokeRequestAckDto>(
                            ea.Body.ToArray())
                        ?? throw new InvalidOperationException(
                            "Ongeldige invoke-acknowledgment.");

                    ackCompletion.TrySetResult(ack);
                }
                catch (Exception ex)
                {
                    ackCompletion.TrySetException(ex);
                }

                return Task.CompletedTask;
            };

            await replyConsumerChannel.BasicConsumeAsync(
                replyQueue.QueueName,
                autoAck: true,
                consumer: consumer,
                cancellationToken: ct);

            await PublishAsync(
                routing,
                data,
                isSendAndForget: false,
                isInvoke: true,
                ct,
                replyTo: replyQueue.QueueName,
                correlationId: correlationId);

            var ack = await ackCompletion.Task.WaitAsync(
                TimeSpan.FromSeconds(100),
                ct);

            if (ack.Exception != null)
                throw new RemoteException(ack.Exception);

            // De responder bepaalt de verbinding waarnaar streamingrequests
            // moeten worden gestuurd. ArgumentIndex -1 is de returnstream.
            var enumerable = RegisterRemoteAsyncEnumerableArgument(
                ack.Routing,
                -1,
                static bytes => bytes);

            await foreach (var item in enumerable.WithCancellation(ct))
                yield return item;
        }
        finally
        {
            await replyConsumerChannel.CloseAsync();
        }
    }

    private async Task PublishAsync(BusRoutingDto routing, byte[] data, bool isSendAndForget, bool isInvoke, CancellationToken ct, string? replyTo = null, string? correlationId = null)
    {
        EnsureInitialized();

        var payload = isInvoke
            ? JsonSerializer.SerializeToUtf8Bytes(
                new BusInvokeRequestDto(routing, data))
            : JsonSerializer.SerializeToUtf8Bytes(
                new BusSendRequestDto(routing, data));

        var properties = new BasicProperties
        {
            Type = isInvoke ? InvokeRequestType : SendRequestType,
            DeliveryMode = deliveryMode,
            ReplyTo = replyTo,
            CorrelationId = correlationId
        };

        var exchange = routing.To switch
        {
            BusToChoice.ToAll => $"ex.{routing.ServiceId}.fanout",
            _ => $"ex.{routing.ServiceId}.direct"
        };

        var routingKey = routing.To switch
        {
            BusToChoice.ToAll => "",
            BusToChoice.ToBusConnectionId =>
                $"connection.{routing.ToBusConnectionId}",
            _ => routing.ServiceId.ToString()
        };

        await PublishRawAsync(
            exchange,
            routingKey,
            payload,
            ct,
            correlationId,
            properties);
    }
    private async Task PublishRawAsync(string exchange, string routingKey, byte[] payload, CancellationToken ct, string? correlationId = null, BasicProperties? properties = null)
    {
        await PublishLock.WaitAsync(ct);

        try
        {
            properties ??= new BasicProperties
            {
                CorrelationId = correlationId
            };

            await _publishChannel!.BasicPublishAsync(
                exchange,
                routingKey,
                mandatory: false,
                basicProperties: properties,
                body: payload,
                cancellationToken: ct);
        }
        finally
        {
            PublishLock.Release();
        }
    }

    private Task SendStreamingRequestAsync(BusRoutingDto routing, int argumentIndex, StreamId streamId, bool cancelled, CancellationToken ct)
    {
        EnsureInitialized();

        var request = new BusStreamingRequestDto(
            routing,
            argumentIndex,
            streamId,
            cancelled);

        var properties = new BasicProperties
        {
            Type = StreamingRequestType,
            ReplyTo = _streamingQueueName
        };

        return PublishRawAsync(
            "",
            $"q.autobus.streaming.{routing.FromBusConnectionId}",
            JsonSerializer.SerializeToUtf8Bytes(request),
            ct,
            properties: properties);
    }

    protected IAsyncEnumerable<T> RegisterRemoteAsyncEnumerableArgument<T>(BusRoutingDto routing, int argumentIndex, Func<byte[], T> deserializer)
    {
        return new RemoteAsyncEnumerable<T>(
            construct: (streamId, enumerator, ct) =>
            {
                var key = new RequestArgumentIndexStreamDto(
                    routing.RequestId,
                    argumentIndex,
                    streamId);

                StreamingResponseHandlers.TryAdd(key, response =>
                {
                    if (response.IsCancelled)
                    {
                        enumerator.Complete(
                            new TaskCanceledException(
                                response.ExceptionMessage));
                        return;
                    }

                    if (response.ExceptionMessage != null)
                    {
                        enumerator.Complete(
                            new RemoteException(response.ExceptionMessage));
                        return;
                    }

                    if (response.IsCompleted)
                    {
                        enumerator.Complete();
                        return;
                    }

                    try
                    {
                        enumerator.Push(
                            deserializer(response.BinaryData));
                    }
                    catch (Exception ex)
                    {
                        enumerator.Complete(ex);
                    }
                });
            },
            requestNext: (streamId, enumerator, ct) =>
                SendStreamingRequestAsync(
                    routing,
                    argumentIndex,
                    streamId,
                    false,
                    ct),
            cancelled: (streamId, enumerator) =>
                SendStreamingRequestAsync(
                    routing,
                    argumentIndex,
                    streamId,
                    true,
                    CancellationToken.None),
            dispose: streamId =>
            {
                StreamingResponseHandlers.TryRemove(
                    new RequestArgumentIndexStreamDto(
                        routing.RequestId,
                        argumentIndex,
                        streamId),
                    out _);
            });
    }

    public Task ReceiveStreamingResponseAsync(BusStreamingResponseDto response, CancellationToken ct)
    {
        var key = new RequestArgumentIndexStreamDto(
            response.Routing.RequestId,
            response.ArgumentIndex,
            response.StreamId);

        if (StreamingResponseHandlers.TryGetValue(key, out var handler))
            handler(response);

        return Task.CompletedTask;
    }

    public void RegisterAsyncEnumerableArgument<T>(BusRoutingDto routing, int argumentIndex, IAsyncEnumerable<T> source, Func<T, byte[]> serializer, CancellationToken cancellationToken)
    {
        StreamingRequestHandlers.TryAdd(
            new RequestArgumentIndexDto(
                routing.RequestId,
                argumentIndex),
            new AsyncEnumerableRegistration<T>(async (
                ConcurrentDictionary<
                    StreamId,
                    AsyncEnumerableRegistrationInstance<T>> activeStreams,
                StreamId streamId,
                bool cancelled,
                CancellationToken ct) =>
            {
                var (enumerator, gate, linkedCts) =
                    activeStreams.GetOrAdd(
                        streamId,
                        _ =>
                        {
                            var linked =
                                CancellationTokenSource.CreateLinkedTokenSource(
                                    cancellationToken,
                                    ct);

                            return new AsyncEnumerableRegistrationInstance<T>(
                                source.GetAsyncEnumerator(linked.Token),
                                new SemaphoreSlim(1, 1),
                                linked);
                        });

                async Task Cleanup()
                {
                    if (activeStreams.TryRemove(streamId, out _))
                    {
                        await enumerator.DisposeAsync();
                        linkedCts.Dispose();
                    }
                }

                var entered = false;

                try
                {
                    await gate.WaitAsync(ct);
                    entered = true;

                    var hasNext =
                        !cancelled && await enumerator.MoveNextAsync();

                    var response = new BusStreamingResponseDto(
                        routing,
                        argumentIndex,
                        streamId,
                        !hasNext,
                        cancelled,
                        null,
                        hasNext ? serializer(enumerator.Current) : []);

                    if (!hasNext || cancelled)
                        await Cleanup();

                    return response;
                }
                catch (OperationCanceledException ex)
                {
                    await Cleanup();

                    return new BusStreamingResponseDto(
                        routing,
                        argumentIndex,
                        streamId,
                        true,
                        true,
                        ex.Message,
                        []);
                }
                catch (Exception ex)
                {
                    await Cleanup();

                    return new BusStreamingResponseDto(
                        routing,
                        argumentIndex,
                        streamId,
                        true,
                        false,
                        ex.Message,
                        []);
                }
                finally
                {
                    if (entered)
                        gate.Release();
                }
            }));
    }

    public async Task UnRegisterAsyncEnumerableArgument(BusRoutingDto routing, int argumentIndex)
    {
        if (StreamingRequestHandlers.TryRemove(
            new RequestArgumentIndexDto(
                routing.RequestId,
                argumentIndex),
            out var registration))
        {
            await registration.DisposeAsync();
        }
    }

    private void EnsureInitialized()
    {
        if (!Initialized ||
            _channel == null ||
            _publishChannel == null ||
            _connection == null)
        {
            throw new InvalidOperationException(
                "Bus verbinding is niet geïnitialiseerd. " +
                "Roep eerst TryConnectAsync aan.");
        }
    }


    private async Task StopConsumersAsync(ServiceConsumerRegistration registration, CancellationToken ct)
    {
        foreach (var consumer in registration.Consumers)
        {
            try
            {
                await consumer.Channel.BasicCancelAsync(
                    consumer.ConsumerTag,
                    cancellationToken: ct);
            }
            finally
            {
                await consumer.Channel.CloseAsync();
            }
        }

        registration.Consumers.Clear();
    }
    private async Task<ConsumerHandle> StartConsumerAsync(string queueName, CancellationToken ct)
    {
        var consumerChannel = await _connection!.CreateChannelAsync(
            cancellationToken: ct);

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
                        case InvokeRequestType:
                            {
                                var request =
                                    JsonSerializer.Deserialize<BusInvokeRequestDto>(body)
                                    ?? throw new InvalidOperationException(
                                        "Ongeldig invoke-verzoek.");

                                var replyTo = ea.BasicProperties.ReplyTo;
                                var correlationId = ea.BasicProperties.CorrelationId;

                                if (string.IsNullOrWhiteSpace(replyTo) ||
                                    string.IsNullOrWhiteSpace(correlationId))
                                {
                                    throw new InvalidOperationException(
                                        "Invoke-verzoek mist ReplyTo of CorrelationId.");
                                }

                                await HandleInvokeRequestAsync(
                                    request,
                                    replyTo,
                                    correlationId,
                                    _lifetimeCts.Token);

                                break;
                            }

                        case SendRequestType:
                            {
                                var request =
                                    JsonSerializer.Deserialize<BusSendRequestDto>(body)
                                    ?? throw new InvalidOperationException(
                                        "Ongeldig send-verzoek.");

                                await Send_SendRequest_ToServiceAsync(
                                    request,
                                    _lifetimeCts.Token);

                                break;
                            }

                        default:
                            throw new InvalidOperationException(
                                $"Onbekend busberichttype: {messageType ?? "(null)"}");
                    }

                    await consumerChannel.BasicAckAsync(
                        ea.DeliveryTag,
                        multiple: false);
                }
                catch
                {
                    await consumerChannel.BasicNackAsync(
                        ea.DeliveryTag,
                        multiple: false,
                        requeue: true);
                }
            };

            var consumerTag = await consumerChannel.BasicConsumeAsync(
                queueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: ct);

            return new ConsumerHandle(consumerChannel, consumerTag);
        }
        catch
        {
            await consumerChannel.CloseAsync();
            throw;
        }
    }
    private async Task HandleInvokeRequestAsync(BusInvokeRequestDto request, string replyTo, string correlationId, CancellationToken ct)
    {
        BusInvokeRequestAckDto ack;

        try
        {
            // De routing voor de returnstream moet naar deze busverbinding wijzen.
            var responseRouting = request.Routing with
            {
                FromBusConnectionId = BusConnectionId,
                To = BusToChoice.ToBusConnectionId,
                ToBusConnectionId = request.Routing.FromBusConnectionId
            };

            // Alleen registreren. De iterator wordt pas opgehaald wanneer
            // de client daadwerkelijk om het eerste item vraagt.
            RegisterAsyncEnumerableArgument<byte[]>(
                request.Routing,
                -1,
                Send_InvokeRequest_ToServiceAsync(
                    request,
                    ct),
                static item => item,
                ct);

            ack = new BusInvokeRequestAckDto(
                responseRouting,
                null);
        }
        catch (Exception ex)
        {
            ack = new BusInvokeRequestAckDto(
                request.Routing with
                {
                    FromBusConnectionId = BusConnectionId,
                    To = BusToChoice.ToBusConnectionId,
                    ToBusConnectionId = request.Routing.FromBusConnectionId
                },
                ex.Message);
        }

        var properties = new BasicProperties
        {
            Type = InvokeAckType,
            CorrelationId = correlationId
        };

        await PublishRawAsync(
            "",
            replyTo,
            JsonSerializer.SerializeToUtf8Bytes(ack),
            ct,
            correlationId,
            properties);
    }
    private async Task StartStreamingConsumerAsync(CancellationToken ct)
    {
        var consumerChannel = await _connection!.CreateChannelAsync(
            cancellationToken: ct);

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
                            {
                                var request =
                                    JsonSerializer.Deserialize<BusStreamingRequestDto>(
                                        body)
                                    ?? throw new InvalidOperationException(
                                        "Ongeldig streamingverzoek.");

                                var key = new RequestArgumentIndexDto(
                                    request.Routing.RequestId,
                                    request.ArgumentIndex);

                                BusStreamingResponseDto response;

                                if (StreamingRequestHandlers.TryGetValue(
                                    key,
                                    out var registration))
                                {
                                    response = await registration.Invoke(
                                        request.StreamId,
                                        request.Cancelled,
                                        _lifetimeCts.Token);
                                }
                                else
                                {
                                    response = new BusStreamingResponseDto(
                                        request.Routing,
                                        request.ArgumentIndex,
                                        request.StreamId,
                                        IsCompleted: true,
                                        IsCancelled: request.Cancelled,
                                        ExceptionMessage: request.Cancelled
                                            ? null
                                            : "De streamingregistratie bestaat niet meer.",
                                        BinaryData: []);
                                }

                                if (!string.IsNullOrWhiteSpace(
                                    ea.BasicProperties.ReplyTo))
                                {
                                    var properties = new BasicProperties
                                    {
                                        Type = StreamingResponseType
                                    };

                                    await PublishRawAsync(
                                        "",
                                        ea.BasicProperties.ReplyTo,
                                        JsonSerializer.SerializeToUtf8Bytes(response),
                                        _lifetimeCts.Token,
                                        properties: properties);
                                }

                                // De returnstream is beëindigd. Ruim de registratie
                                // op nadat de response is aangemaakt.
                                if (request.ArgumentIndex == -1 &&
                                    (response.IsCompleted ||
                                     response.IsCancelled ||
                                     response.ExceptionMessage != null))
                                {
                                    await UnRegisterAsyncEnumerableArgument(
                                        request.Routing,
                                        -1);
                                }

                                break;
                            }

                        case StreamingResponseType:
                            {
                                var response =
                                    JsonSerializer.Deserialize<BusStreamingResponseDto>(
                                        body)
                                    ?? throw new InvalidOperationException(
                                        "Ongeldig streamingantwoord.");

                                await ReceiveStreamingResponseAsync(
                                    response,
                                    _lifetimeCts.Token);

                                break;
                            }

                        default:
                            throw new InvalidOperationException(
                                $"Onbekend streamingberichttype: " +
                                $"{ea.BasicProperties.Type ?? "(null)"}");
                    }

                    await consumerChannel.BasicAckAsync(
                        ea.DeliveryTag,
                        multiple: false);
                }
                catch
                {
                    await consumerChannel.BasicNackAsync(
                        ea.DeliveryTag,
                        multiple: false,
                        requeue: true);
                }
            };

            _streamingConsumerTag = await consumerChannel.BasicConsumeAsync(
                _streamingQueueName!,
                autoAck: false,
                consumer: consumer,
                cancellationToken: ct);

            _streamingConsumerChannel = consumerChannel;
        }
        catch
        {
            await consumerChannel.CloseAsync();
            throw;
        }
    }



    protected abstract Task Send_SendRequest_ToServiceAsync(BusSendRequestDto sendRequest, CancellationToken ct);

    protected abstract IAsyncEnumerable<byte[]> Send_InvokeRequest_ToServiceAsync(BusInvokeRequestDto invokeRequest, CancellationToken ct);





    public async ValueTask DisposeAsync()
    {
        _lifetimeCts.Cancel();

        await SubscriptionLock.WaitAsync();

        try
        {
            foreach (var registration in ServiceConsumers.Values)
                await StopConsumersAsync(
                    registration,
                    CancellationToken.None);

            ServiceConsumers.Clear();
        }
        finally
        {
            SubscriptionLock.Release();
        }

        foreach (var registration in StreamingRequestHandlers.Values)
            await registration.DisposeAsync();

        StreamingRequestHandlers.Clear();
        StreamingResponseHandlers.Clear();

        if (_streamingConsumerChannel != null)
        {
            if (_streamingConsumerTag != null)
            {
                await _streamingConsumerChannel.BasicCancelAsync(
                    _streamingConsumerTag,
                    cancellationToken: CancellationToken.None);
            }

            await _streamingConsumerChannel.CloseAsync();
        }

        if (_publishChannel != null)
            await _publishChannel.CloseAsync();

        if (_channel != null)
            await _channel.CloseAsync();

        if (_connection != null)
            await _connection.CloseAsync();

        _lifetimeCts.Dispose();
        SubscriptionLock.Dispose();
        PublishLock.Dispose();

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
