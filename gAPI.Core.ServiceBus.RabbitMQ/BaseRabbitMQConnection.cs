using gAPI.Core.Dtos;
using gAPI.Core.Enums; // Vereist voor streaming data overdracht
using gAPI.Core.Helpers;
using gAPI.Core.Ids;
using gAPI.Core.ServiceBus.Config;
using gAPI.Core.ServiceBus.Helpers;
using gAPI.Core.ServiceBus.Interfaces;
using global::RabbitMQ.Client;
using global::RabbitMQ.Client.Events;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;

namespace gAPI.Core.ServiceBus.RabbitMQ;

public class BaseRabbitMQConnection(
    ServiceBusConfig busConfig) 
    : IBaseBusConnection
    , IAsyncDisposable
{
    private readonly ConcurrentDictionary<RequestArgumentIndexStreamDto, Action<BusStreamingResponseDto>> StreamingResponseHandlers = [];
    private readonly ConcurrentDictionary<RequestArgumentIndexDto, IAsyncEnumerableRegistration> StreamingRequestHandlers = [];
    private IConnection? _connection;
    private IChannel? _channel;
    private string? _streamingQueueName;

    // Houdt nu de actieve reply-to queues bij per actieve inkomende invoke stream
    private readonly ConcurrentDictionary<string, string> _rpcReplyToTracker = new();

    // EOS (End of Stream) marker: een lege byte array geeft aan dat de IAsyncEnumerable klaar is
    private static readonly byte[] EndOfStreamMarker = Array.Empty<byte>();

    public bool Initialized { get; private set; }

    public BusConnectionId BusConnectionId { get; private set; } = BusConnectionId.New();

    protected virtual Task Send_SendRequest_ToServiceAsync(BusSendRequestDto sendRequest, CancellationToken ct)
    {
        return Task.CompletedTask;
    }
    protected virtual IAsyncEnumerable<byte[]> Send_InvokeRequest_ToServiceAsync(BusInvokeRequestDto invokeRequest, CancellationToken ct)
    {
        return AsyncEnumerable.Empty<byte[]>();
    }

    public async Task TryConnectAsync(CancellationToken ct)
    {
        if (Initialized) return;

        var factory = new ConnectionFactory
        {
            HostName = busConfig.HostName,
            UserName = busConfig.UserName,
            Password = busConfig.Password,
            //DispatchConsumersAsync = true
        };

        _connection = await factory.CreateConnectionAsync(ct);
        _channel = await _connection.CreateChannelAsync(cancellationToken: ct);

        Initialized = true;
    }

    protected async Task SendSubscribeAsync(object service, ServiceId serviceId, CancellationToken ct)
    {
        EnsureInitialized();

        var directExchange = $"ex.{serviceId}.direct";
        var fanoutExchange = $"ex.{serviceId}.fanout";

        await _channel!.ExchangeDeclareAsync(directExchange, ExchangeType.Direct, durable: true, cancellationToken: ct);
        await _channel!.ExchangeDeclareAsync(fanoutExchange, ExchangeType.Fanout, durable: true, cancellationToken: ct);

        var sharedQueue = $"q.{serviceId}.any";
        await _channel.QueueDeclareAsync(sharedQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await _channel.QueueBindAsync(sharedQueue, directExchange, routingKey: serviceId.ToString(), cancellationToken: ct);

        var instanceQueue = $"q.{serviceId}.all.{BusConnectionId}";
        await _channel.QueueDeclareAsync(instanceQueue, durable: false, exclusive: false, autoDelete: true, cancellationToken: ct);
        await _channel.QueueBindAsync(instanceQueue, fanoutExchange, routingKey: "", cancellationToken: ct);

        await StartConsumerAsync(sharedQueue, ct);
        await StartConsumerAsync(instanceQueue, ct);
    }
    protected async Task SendUnsubscribeAsync(object service, ServiceId serviceId, CancellationToken ct)
    {
        EnsureInitialized();
        var instanceQueue = $"q.{serviceId}.all.{BusConnectionId}";
        await _channel!.QueueDeleteAsync(instanceQueue, cancellationToken: ct);
    }
    private async Task StartConsumerAsync(string queueName, CancellationToken ct)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel!);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var bodyBytes = ea.Body.ToArray();
                var correlationId = ea.BasicProperties.CorrelationId;

                if (!string.IsNullOrEmpty(ea.BasicProperties.ReplyTo) && !string.IsNullOrEmpty(correlationId))
                {
                    var invokeRequest = JsonSerializer.Deserialize<BusInvokeRequestDto>(bodyBytes);
                    if (invokeRequest != null)
                    {
                        // Registreer de reply queue voor deze specifieke stream
                        _rpcReplyToTracker[correlationId] = ea.BasicProperties.ReplyTo;

                        // Doorloop de IAsyncEnumerable van de gegenereerde reader live
                        await foreach (var responseChunk in Send_InvokeRequest_ToServiceAsync(invokeRequest, ct).WithCancellation(ct))
                        {
                            // Schiet elke chunk direct terug over de bus naar de vragende partij
                            await SendRpcChunkAsync(correlationId, responseChunk, ct);
                        }

                        // Zend de End of Stream (EOS) marker zodra de C# enumerable klaar is
                        await SendRpcChunkAsync(correlationId, EndOfStreamMarker, ct);

                        // Ruim de tracker voor dit correlationId netjes op
                        _rpcReplyToTracker.TryRemove(correlationId, out _);
                    }
                }
                else
                {
                    var sendRequest = JsonSerializer.Deserialize<BusSendRequestDto>(bodyBytes);
                    if (sendRequest != null)
                    {
                        await Send_SendRequest_ToServiceAsync(sendRequest, ct);
                    }
                }

                await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch
            {
                await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        await _channel!.BasicConsumeAsync(queueName, autoAck: false, consumer: consumer, cancellationToken: ct);
    }
    private async Task SendRpcChunkAsync(string correlationId, byte[] chunkData, CancellationToken ct)
    {
        // We gebruiken TryGetValue in plaats van TryRemove, omdat we gedurende de stream
        // meerdere malen naar dezelfde queue moeten kunnen publiceren.
        if (_rpcReplyToTracker.TryGetValue(correlationId, out var replyToQueue))
        {
            var props = new BasicProperties { CorrelationId = correlationId };
            await _channel!.BasicPublishAsync(
            exchange: "",
            routingKey: replyToQueue,
            mandatory: true,
            basicProperties: props,
            body: chunkData,
            cancellationToken: ct);
        }
    }

    public async Task SendFireAndForgetAsync(BusRoutingDto routing, byte[] data, CancellationToken ct)
    {
        await PublishAsync(routing, data, isInvoke: false, ct);
    }
    public async Task SendAsync(BusRoutingDto routing, byte[] data, CancellationToken ct)
    {
        await PublishAsync(routing, data, isInvoke: false, ct);
    }
    public async IAsyncEnumerable<byte[]> InvokeAsync(BusRoutingDto routing, byte[] data, [EnumeratorCancellation] CancellationToken ct)
    {
        EnsureInitialized();

        var correlationId = routing.RequestId.ToString();

        // We gebruiken een onbegrensd kanaal om alle binnenkomende chunks asynchroon te bufferen
        var responseChannel = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions
        {
            SingleWriter = true,
            SingleReader = true
        });

        var replyQueueResult = await _channel!.QueueDeclareAsync(queue: "", durable: false, exclusive: true, autoDelete: true, cancellationToken: ct);
        var replyQueueName = replyQueueResult.QueueName;

        var replyConsumer = new AsyncEventingBasicConsumer(_channel);
        replyConsumer.ReceivedAsync += (sender, ea) =>
        {
            if (ea.BasicProperties.CorrelationId == correlationId)
            {
                var payload = ea.Body.ToArray();

                // Controleren of dit de End of Stream (EOS) marker is
                if (payload.Length == 0)
                {
                    responseChannel.Writer.TryComplete();
                }
                else
                {
                    responseChannel.Writer.TryWrite(payload);
                }
            }
            return Task.CompletedTask;
        };

        await _channel.BasicConsumeAsync(replyQueueName, autoAck: true, consumer: replyConsumer, cancellationToken: ct);

        // Verstuur de initiate call naar de bus
        await PublishAsync(routing, data, isInvoke: true, ct, replyQueueName, correlationId);

        // Lees de chunks live uit het kanaal zodra RabbitMQ ze binnenschiet
        while (await responseChannel.Reader.WaitToReadAsync(ct))
        {
            while (responseChannel.Reader.TryRead(out var chunk))
            {
                yield return chunk;
            }
        }

        // Ruim de tijdelijke queue netjes op zodra de stroom compleet is
        await _channel.QueueDeleteAsync(replyQueueName, cancellationToken: ct);
    }

    private async Task PublishAsync(BusRoutingDto routing, byte[] data, bool isInvoke, CancellationToken ct, string? replyTo = null, string? correlationId = null)
    {
        EnsureInitialized();

        var exchange = routing.To == BusToChoice.ToAll ? $"ex.{routing.ServiceId}.fanout" : $"ex.{routing.ServiceId}.direct";
        var routingKey = routing.To == BusToChoice.ToAll ? "" : routing.ServiceId.ToString();

        byte[] payload;
        if (isInvoke)
        {
            payload = JsonSerializer.SerializeToUtf8Bytes(new BusInvokeRequestDto(routing, data));
        }
        else
        {
            payload = JsonSerializer.SerializeToUtf8Bytes(new BusSendRequestDto(routing, data));
        }

        var properties = new BasicProperties
        {
            DeliveryMode = isInvoke ? DeliveryModes.Persistent : DeliveryModes.Transient
        };

        if (replyTo != null) properties.ReplyTo = replyTo;
        if (correlationId != null) properties.CorrelationId = correlationId;

        await _channel!.BasicPublishAsync(
            exchange: exchange,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: payload,
            cancellationToken: ct);
    }
    private void EnsureInitialized()
    {
        if (!Initialized || _channel == null)
            throw new InvalidOperationException("Bus verbinding is niet geïnitialiseerd. Roep eerst TryConnectAsync aan.");
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel != null) await _channel.CloseAsync();
        if (_connection != null) await _connection.CloseAsync();
        GC.SuppressFinalize(this);
    }


    // Dit is de ontbrekende schakel die de RemoteAsyncEnumerable aanroept!
    private async Task SendStreamingRequestAsync(BusRoutingDto routing, int argumentIndex, StreamId streamId, bool cancelled, CancellationToken ct)
    {
        if (!Initialized || _channel == null) throw new InvalidOperationException("Niet verbonden.");

        // We sturen een verzoek terug naar de AFZENDER van de originele call
        // Om te vragen naar de volgende chunk van het argument
        var streamingRequest = new BusStreamingRequestDto(
            routing,
            argumentIndex,
            streamId,
            cancelled//,
            //_streamingQueueName! // Vertel de zender waar de response heen moet
        );

        var body = JsonSerializer.SerializeToUtf8Bytes(streamingRequest);

        // We publiceren dit specifiek naar de direct exchange van de service, 
        // of via een centrale streaming exchange. We gebruiken hier de direct van de service.
        await _channel.BasicPublishAsync(
            exchange: $"ex.{routing.ServiceId}.direct",
            routingKey: routing.ServiceId.ToString(),
            mandatory: true,
            body: body,
            cancellationToken: ct);
    }

    //// Wordt opgeroepen bij TryConnectAsync om de wereldwijde streaming-poort voor deze machine te openen
    //private async Task SetupStreamingInfrastructuurAsync(CancellationToken ct)
    //{
    //    // Maak een unieke queue aan voor ALLE inkomende stream-antwoorden richting deze applicatie-instantie
    //    var result = await _channel!.QueueDeclareAsync(
    //        queue: $"q.autorabbit.streaming.{BusConnectionId}",
    //        durable: false,
    //        exclusive: true,
    //        autoDelete: true,
    //        cancellationToken: ct);

    //    _streamingQueueName = result.QueueName;

    //    var consumer = new AsyncEventingBasicConsumer(_channel);
    //    consumer.ReceivedAsync += async (sender, ea) =>
    //    {
    //        var bodyBytes = ea.Body.ToArray();

    //        // Is dit een RESPONSE op een argument waar wij (de ontvanger) om vroegen?
    //        if (TryDeserialize<BusStreamingResponseDto>(bodyBytes, out var response) && response != null)
    //        {
    //            var key = new RequestArgumentIndexStreamDto(response.Routing.RequestId, response.ArgumentIndex, response.StreamId);
    //            if (StreamingResponseHandlers.TryGetValue(key, out var handler))
    //            {
    //                handler.Invoke(response);
    //            }
    //        }
    //        // Of is dit een REQUEST van iemand anders die een item uit ONZE (zender) IEnumerable wil trekken?
    //        else if (TryDeserialize<BusStreamingRequestDto>(bodyBytes, out var request) && request != null)
    //        {
    //            var requestKey = new RequestArgumentIndexDto(request.RequestId, request.ArgumentIndex);
    //            if (StreamingRequestHandlers.TryGetValue(requestKey, out var registration))
    //            {
    //                // Trek de data uit de echte C# enumerator (jouw gegenereerde code heeft dit geregistreerd!)
    //                var responseDto = await registration.HandleRequestAsync(request.StreamId, request.Cancelled, ct);

    //                // Stuur de gevraagde byte-chunk terug naar de streaming-queue van de vrager
    //                var responseBytes = JsonSerializer.SerializeToUtf8Bytes(responseDto);
    //                await _channel.BasicPublishAsync(
    //                    exchange: "",
    //                    routingKey: request.ReplyToStreamingQueue,
    //                    body: responseBytes,
    //                    cancellationToken: ct);
    //            }
    //        }

    //        await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
    //    };

    //    await _channel.BasicConsumeAsync(_streamingQueueName, autoAck: false, consumer: consumer, cancellationToken: ct);
    //}


    // This is called on the receiving end to make a instance for remote streaming
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

    // These are called from the sender
    public void RegisterAsyncEnumerableArgument<T>(BusRoutingDto routing, int argumentIndex, IAsyncEnumerable<T> source, Func<T, byte[]> serializer, CancellationToken cancellationToken)
    {
        StreamingRequestHandlers.TryAdd(new(routing.RequestId, argumentIndex), new AsyncEnumerableRegistration<T>(async (
            ConcurrentDictionary<StreamId, AsyncEnumerableRegistrationInstance<T>> activeStreams,
            StreamId streamId,
            bool cancelled,
            CancellationToken ct) =>
        {
            var (enumerator, gate, linkedCts) = activeStreams.GetOrAdd(
                streamId,
                _ =>
                {
                    var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, ct);
                    return new AsyncEnumerableRegistrationInstance<T>(
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

                var response = new BusStreamingResponseDto(
                    routing,
                    argumentIndex,
                    streamId,
                    !hasNext,
                    cancelled,
                    null,
                    hasNext ? serializer(enumerator.Current) : []);

                if (!hasNext)
                    await Cleanup();

                return response;
            }
            catch (OperationCanceledException ex) when (
                ct.IsCancellationRequested ||
                cancellationToken.IsCancellationRequested)
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
        if (StreamingRequestHandlers.TryRemove(new(routing.RequestId, argumentIndex), out var registration))
        {
            await registration.DisposeAsync();
        }
    }
}