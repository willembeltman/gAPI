//using global::RabbitMQ.Client;
//using global::RabbitMQ.Client.Events;
//using gAPI.Core.Dtos;
//using gAPI.Core.Ids;
//using gAPI.Core.ServiceBus.Config;
//using gAPI.Core.ServiceBus.Interfaces;
//using System.Collections.Concurrent;
//using System.Runtime.CompilerServices;
//using System.Text.Json;
//using System.Threading.Channels; // Vereist voor streaming data overdracht

//namespace gAPI.Core.ServiceBus.Bus;

//public class RabbitMQConnection : IBaseBusConnection, IAsyncDisposable
//{
//    private IConnection? _connection;
//    private IChannel? _channel;
//    private readonly ServiceBusConfig _busConfig;
//    private readonly string _instanceId = Guid.NewGuid().ToString("N");

//    // Houdt nu de actieve reply-to queues bij per actieve inkomende invoke stream
//    private readonly ConcurrentDictionary<string, string> _rpcReplyToTracker = new();

//    // EOS (End of Stream) marker: een lege byte array geeft aan dat de IAsyncEnumerable klaar is
//    private static readonly byte[] EndOfStreamMarker = Array.Empty<byte>();

//    public bool Initialized { get; private set; }

//    public RabbitMQConnection(ServiceBusConfig busConfig)
//    {
//        _busConfig = busConfig;
//    }

//    protected virtual Task Send_SendRequest_ToServiceAsync(BusSendRequestDto sendRequest, CancellationToken ct)
//    {
//        return Task.CompletedTask;
//    }
//    protected virtual IAsyncEnumerable<byte[]> Send_InvokeRequest_ToServiceAsync(BusInvokeRequestDto invokeRequest, CancellationToken ct)
//    {
//        return AsyncEnumerable.Empty<byte[]>();
//    }

//    public async Task TryConnectAsync(CancellationToken ct)
//    {
//        if (Initialized) return;

//        var factory = new ConnectionFactory
//        {
//            HostName = _busConfig.HostName,
//            UserName = _busConfig.UserName,
//            Password = _busConfig.Password,
//            //DispatchConsumersAsync = true
//        };

//        _connection = await factory.CreateConnectionAsync(ct);
//        _channel = await _connection.CreateChannelAsync(cancellationToken: ct);

//        Initialized = true;
//    }

//    protected async Task SendSubscribeAsync(ServiceId serviceId, CancellationToken ct)
//    {
//        EnsureInitialized();

//        var directExchange = $"ex.{serviceId}.direct";
//        var fanoutExchange = $"ex.{serviceId}.fanout";

//        await _channel!.ExchangeDeclareAsync(directExchange, ExchangeType.Direct, durable: true, cancellationToken: ct);
//        await _channel!.ExchangeDeclareAsync(fanoutExchange, ExchangeType.Fanout, durable: true, cancellationToken: ct);

//        var sharedQueue = $"q.{serviceId}.any";
//        await _channel.QueueDeclareAsync(sharedQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
//        await _channel.QueueBindAsync(sharedQueue, directExchange, routingKey: serviceId.ToString(), cancellationToken: ct);

//        var instanceQueue = $"q.{serviceId}.all.{_instanceId}";
//        await _channel.QueueDeclareAsync(instanceQueue, durable: false, exclusive: false, autoDelete: true, cancellationToken: ct);
//        await _channel.QueueBindAsync(instanceQueue, fanoutExchange, routingKey: "", cancellationToken: ct);

//        await StartConsumerAsync(sharedQueue, ct);
//        await StartConsumerAsync(instanceQueue, ct);
//    }
//    protected async Task SendUnsubscribeAsync(ServiceId serviceId, CancellationToken ct)
//    {
//        EnsureInitialized();
//        var instanceQueue = $"q.{serviceId}.all.{_instanceId}";
//        await _channel!.QueueDeleteAsync(instanceQueue, cancellationToken: ct);
//    }
//    private async Task StartConsumerAsync(string queueName, CancellationToken ct)
//    {
//        var consumer = new AsyncEventingBasicConsumer(_channel!);
//        consumer.ReceivedAsync += async (model, ea) =>
//        {
//            try
//            {
//                var bodyBytes = ea.Body.ToArray();
//                var correlationId = ea.BasicProperties.CorrelationId;

//                if (!string.IsNullOrEmpty(ea.BasicProperties.ReplyTo) && !string.IsNullOrEmpty(correlationId))
//                {
//                    var invokeRequest = JsonSerializer.Deserialize<BusInvokeRequestDto>(bodyBytes);
//                    if (invokeRequest != null)
//                    {
//                        // Registreer de reply queue voor deze specifieke stream
//                        _rpcReplyToTracker[correlationId] = ea.BasicProperties.ReplyTo;

//                        // Doorloop de IAsyncEnumerable van de gegenereerde reader live
//                        await foreach (var responseChunk in Send_InvokeRequest_ToServiceAsync(invokeRequest, ct).WithCancellation(ct))
//                        {
//                            // Schiet elke chunk direct terug over de bus naar de vragende partij
//                            await SendRpcChunkAsync(correlationId, responseChunk, ct);
//                        }

//                        // Zend de End of Stream (EOS) marker zodra de C# enumerable klaar is
//                        await SendRpcChunkAsync(correlationId, EndOfStreamMarker, ct);

//                        // Ruim de tracker voor dit correlationId netjes op
//                        _rpcReplyToTracker.TryRemove(correlationId, out _);
//                    }
//                }
//                else
//                {
//                    var sendRequest = JsonSerializer.Deserialize<BusSendRequestDto>(bodyBytes);
//                    if (sendRequest != null)
//                    {
//                        await Send_SendRequest_ToServiceAsync(sendRequest, ct);
//                    }
//                }

//                await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
//            }
//            catch
//            {
//                await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
//            }
//        };

//        await _channel!.BasicConsumeAsync(queueName, autoAck: false, consumer: consumer, cancellationToken: ct);
//    }
//    private async Task SendRpcChunkAsync(string correlationId, byte[] chunkData, CancellationToken ct)
//    {
//        // We gebruiken TryGetValue in plaats van TryRemove, omdat we gedurende de stream
//        // meerdere malen naar dezelfde queue moeten kunnen publiceren.
//        if (_rpcReplyToTracker.TryGetValue(correlationId, out var replyToQueue))
//        {
//            var props = new BasicProperties { CorrelationId = correlationId };
//            await _channel!.BasicPublishAsync(
//            exchange: "",
//            routingKey: replyToQueue,
//            mandatory: true,
//            basicProperties: props,
//            body: chunkData,
//            cancellationToken: ct);
//        }
//    }

//    public async Task SendFireAndForgetAsync(BusRoutingDto routing, byte[] data, CancellationToken ct)
//    {
//        await PublishAsync(routing, data, isInvoke: false, ct);
//    }
//    public async Task SendAsync(BusRoutingDto routing, byte[] data, CancellationToken ct)
//    {
//        await PublishAsync(routing, data, isInvoke: false, ct);
//    }
//    public async IAsyncEnumerable<byte[]> InvokeAsync(BusRoutingDto routing, byte[] data, [EnumeratorCancellation] CancellationToken ct)
//    {
//        EnsureInitialized();

//        var correlationId = routing.RequestId.ToString();

//        // We gebruiken een onbegrensd kanaal om alle binnenkomende chunks asynchroon te bufferen
//        var responseChannel = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions
//        {
//            SingleWriter = true,
//            SingleReader = true
//        });

//        var replyQueueResult = await _channel!.QueueDeclareAsync(queue: "", durable: false, exclusive: true, autoDelete: true, cancellationToken: ct);
//        var replyQueueName = replyQueueResult.QueueName;

//        var replyConsumer = new AsyncEventingBasicConsumer(_channel);
//        replyConsumer.ReceivedAsync += (sender, ea) =>
//        {
//            if (ea.BasicProperties.CorrelationId == correlationId)
//            {
//                var payload = ea.Body.ToArray();

//                // Controleren of dit de End of Stream (EOS) marker is
//                if (payload.Length == 0)
//                {
//                    responseChannel.Writer.TryComplete();
//                }
//                else
//                {
//                    responseChannel.Writer.TryWrite(payload);
//                }
//            }
//            return Task.CompletedTask;
//        };

//        await _channel.BasicConsumeAsync(replyQueueName, autoAck: true, consumer: replyConsumer, cancellationToken: ct);

//        // Verstuur de initiate call naar de bus
//        await PublishAsync(routing, data, isInvoke: true, ct, replyQueueName, correlationId);

//        // Lees de chunks live uit het kanaal zodra RabbitMQ ze binnenschiet
//        while (await responseChannel.Reader.WaitToReadAsync(ct))
//        {
//            while (responseChannel.Reader.TryRead(out var chunk))
//            {
//                yield return chunk;
//            }
//        }

//        // Ruim de tijdelijke queue netjes op zodra de stroom compleet is
//        await _channel.QueueDeleteAsync(replyQueueName, cancellationToken: ct);
//    }

//    private async Task PublishAsync(BusRoutingDto routing, byte[] data, bool isInvoke, CancellationToken ct, string? replyTo = null, string? correlationId = null)
//    {
//        EnsureInitialized();

//        var exchange = routing.ToAll ? $"ex.{routing.ServiceId}.fanout" : $"ex.{routing.ServiceId}.direct";
//        var routingKey = routing.ToAll ? "" : routing.ServiceId.ToString();

//        byte[] payload;
//        if (isInvoke)
//        {
//            payload = JsonSerializer.SerializeToUtf8Bytes(new BusInvokeRequestDto(routing, data));
//        }
//        else
//        {
//            payload = JsonSerializer.SerializeToUtf8Bytes(new BusSendRequestDto(routing, data));
//        }

//        var properties = new BasicProperties
//        {
//            DeliveryMode = isInvoke ? DeliveryModes.Persistent : DeliveryModes.Transient
//        };

//        if (replyTo != null) properties.ReplyTo = replyTo;
//        if (correlationId != null) properties.CorrelationId = correlationId;

//        await _channel!.BasicPublishAsync(
//            exchange: exchange,
//            routingKey: routingKey,
//            mandatory: false,
//            basicProperties: properties,
//            body: payload,
//            cancellationToken: ct);
//    }
//    private void EnsureInitialized()
//    {
//        if (!Initialized || _channel == null)
//            throw new InvalidOperationException("Bus verbinding is niet geïnitialiseerd. Roep eerst TryConnectAsync aan.");
//    }

//    public async ValueTask DisposeAsync()
//    {
//        if (_channel != null) await _channel.CloseAsync();
//        if (_connection != null) await _connection.CloseAsync();
//        GC.SuppressFinalize(this);
//    }
//}