using gAPI.Core.Client.Config;
using gAPI.Core.Dtos;
using gAPI.Core.Enums;
using gAPI.Core.Ids;
using gAPI.Core.Interfaces;
using gAPI.Core.Serializers;
using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Net.WebSockets;
using System.Threading.Channels;

namespace gAPI.Core.Client.Wss;

public class WssClientConnectionSender(
    WssClientConnection wssClientConnection,
    ClientConfig config,
    IClientLoggerFactory LoggerFactory)
{
    private readonly ILogger<WssClientConnectionSender> Logger = LoggerFactory.CreateLogger<WssClientConnectionSender>();
    private readonly byte[] SendBuffer = new byte[config.MaxPackageSize ?? 512 * 1024];
    private Channel<SendQueueItem>? SendQueue;

    public async Task SendKernel(WebSocket socket, InitializeDto initialize, CancellationToken ct)
    {
        try
        {
            var sendQueue = Channel.CreateUnbounded<SendQueueItem>();
            SendQueue = sendQueue;

            var span = SendBuffer.AsSpan();
            var offset = 0;

            span.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.Initialize);
            span.Write(ref offset, initialize);

            await socket.SendAsync(
                new ArraySegment<byte>(SendBuffer, 0, offset),
                WebSocketMessageType.Binary,
                true,
                ct);

            await foreach (var item in sendQueue.Reader.ReadAllAsync(ct))
            {
                try
                {
                    span = SendBuffer.AsSpan();
                    offset = item.Write(span);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "SendKernel");
                    item.Sent?.TrySetException(ex);
                    throw;
                }

                // Zodra item.Write(span) klaar is, zijn de bytes gekopieerd naar SendBuffer.
                // De caller mag zijn gehuurde ArrayPool-buffer nu direct weer veilig teruggeven!
                item.Sent?.TrySetResult();

                await socket.SendAsync(
                    new ArraySegment<byte>(SendBuffer, 0, offset),
                    WebSocketMessageType.Binary,
                    true,
                    ct);
            }
        }
        catch (OperationCanceledException ex)
        {
            // Hier komt de cancel vanuit cts.Cancel() bij disconnect, gewoon negeren
            var sendQueue = SendQueue;
            if (sendQueue != null)
                CompleteQueue(sendQueue, ex);
        }
        catch (Exception ex)
        {
            // A failed send means this connection can no longer drain the queue.
            // Do not hide it: the connection owner must cancel the receiver and
            // dispose its subscriptions instead of accepting messages forever.
            Logger.LogError(ex, "WSS sender stopped for client {ClientConnectionId}", wssClientConnection.ClientConnectionId);
            var sendQueue = SendQueue;
            if (sendQueue != null) 
                CompleteQueue(sendQueue, ex);
        }
    }
    private void CompleteQueue(Channel<SendQueueItem> sendQueue, Exception error)
    {
        SendQueue = null;

        sendQueue.Writer.TryComplete(error);

        while (sendQueue.Reader.TryRead(out var item))
            item.Sent?.TrySetException(error);
    }

    private async Task EnqueueAsync(Func<Span<byte>, int> write, CancellationToken ct)
    {
        var sendQueue = SendQueue;
        while (sendQueue == null)
        {
            await Task.Delay(100);
            sendQueue = SendQueue;
        }

        if (!sendQueue.Writer.TryWrite(new SendQueueItem(write, null)))
        {
            return;
        }
    }
    private async Task EnqueueAndWaitAsync(Func<Span<byte>, int> write, CancellationToken ct)
    {
        var sendQueue = SendQueue;
        while (sendQueue == null)
        {
            await Task.Delay(100);
            sendQueue = SendQueue;
        }

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        if (!sendQueue.Writer.TryWrite(new SendQueueItem(write, completion)))
            throw new Exception("Not connected");

        await completion.Task.WaitAsync(ct).ConfigureAwait(false);
    }

    public async Task Send_Subscribe_ToServerAsync(SubscribeDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_Subscribe_ToServerAsync({serviceId}, {sessionId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.ServiceId, message.SessionId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_Subscribe_ToServerAsync({serviceId}, {sessionId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.ServiceId, message.SessionId);

            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.Subscribe);
            writer.Write(ref offset, message);
            return offset;
        }, ct);
    }
    public async Task Send_Unsubscribe_ToServerAsync(UnsubscribeDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_Unsubscribe_ToServerAsync({serviceId}, {sessionId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.ServiceId, message.SessionId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_Unsubscribe_ToServerAsync({serviceId}, {sessionId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.ServiceId, message.SessionId);

            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.Unsubscribe);
            writer.Write(ref offset, message);
            return offset;
        }, ct);
    }

    public async Task Send_SendAndForgetRequest_ToServerAsync(SendRequestClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_SendAndForgetRequest_ToServerAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        await EnqueueAndWaitAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_SendAndForgetRequest_ToServerAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.SendAndForgetRequest);
            writer.Write(ref offset, message);

            return offset;
        }, ct);
    }
    public async Task Send_SendRequest_ToServerAsync(SendRequestClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_SendRequest_ToServerAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        await EnqueueAndWaitAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_SendRequest_ToServerAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.SendRequest);
            writer.Write(ref offset, message);

            return offset;
        }, ct);
    }
    public async Task Send_SendRequestCancelled_ToServerAsync(SendRequestCancelledClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_SendRequestCancelled_ToServerAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_SendRequestCancelled_ToServerAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.SendRequestCancelled);
            writer.Write(ref offset, message);
            return offset;
        }, ct);
    }
    public async Task Send_SendRequestDone_ToServerAsync(SendRequestDoneClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_SendRequestDone_ToServerAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_SendRequestDone_ToServerAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.FabricSendRequestDone);
            writer.Write(ref offset, message);
            return offset;
        }, ct);
    }

    public async Task Send_InvokeRequest_ToServerAsync(InvokeRequestClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_InvokeRequest_ToServerAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        await EnqueueAndWaitAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_InvokeRequest_ToServerAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.InvokeRequest);
            writer.Write(ref offset, message);
            return offset;
        }, ct);
    }
    public async Task Send_InvokeRequestCancelled_ToServerAsync(InvokeRequestCancelledClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_InvokeCancelled_ToServerAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_InvokeCancelled_ToServerAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.InvokeRequestCancelled);
            writer.Write(ref offset, message);
            return offset;
        }, ct);
    }
    public async Task Send_InvokeRequestAck_ToServerAsync(InvokeRequestAckClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_InvokeRequestAck_ToServerAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_InvokeRequestAck_ToServerAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.FabricInvokeRequestAck);
            writer.Write(ref offset, message);
            return offset;
        }, ct);
    }

    public async Task Send_StreamingRequest_ToServerAsync(StreamingRequestClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_StreamingRequest_ToServerAsync({RequestId}, {ServiceId}, {MethodId}, {index})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId, message.ArgumentIndex);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_StreamingRequest_ToServerAsync({RequestId}, {ServiceId}, {MethodId}, {index})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId, message.ArgumentIndex);

            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.StreamingRequest);
            writer.Write(ref offset, message);
            return offset;
        }, ct);
    }
    public async Task Send_StreamingResponse_ToServerAsync(StreamingResponseClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_StreamingResponse_ToServerAsync({RequestId}, {ServiceId}, {MethodId}, {index})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId, message.ArgumentIndex);

        await EnqueueAndWaitAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_StreamingResponse_ToServerAsync({RequestId}, {ServiceId}, {MethodId}, {index})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId, message.ArgumentIndex);

            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.StreamingResponse);
            writer.Write(ref offset, message);
            return offset;
        }, ct);
    }

    public async Task Send_FabricStreamingRequest_ToServerAsync(StreamingRequestClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_FabricStreamingRequest_ToServerAsync({RequestId}, {ServiceId}, {MethodId}, {index})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId, message.ArgumentIndex);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_FabricStreamingRequest_ToServerAsync({RequestId}, {ServiceId}, {MethodId}, {index})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId, message.ArgumentIndex);

            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.FabricStreamingRequest);
            writer.Write(ref offset, message);
            return offset;
        }, ct);
    }
    public async Task Send_FabricStreamingResponse_ToServerAsync(StreamingResponseClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: QUEUED Send_FabricStreamingResponse_ToServerAsync({RequestId}, {ServiceId}, {MethodId}, {index})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId, message.ArgumentIndex);

        await EnqueueAndWaitAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Send_FabricStreamingResponse_ToServerAsync({RequestId}, {ServiceId}, {MethodId}, {index})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId, message.ArgumentIndex);

            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.FabricStreamingResponse);
            writer.Write(ref offset, message);
            return offset;
        }, ct);
    }

    public async Task Send_Log_ToServerAsync(ClientLogDto log, CancellationToken ct)
    {
        // NIET LOGGEN :)
        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.Log);
            writer.Write(ref offset, log);
            return offset;
        }, ct);
    }

    private sealed record SendQueueItem(
        Func<Span<byte>, int> Write,
        TaskCompletionSource? Sent);

}