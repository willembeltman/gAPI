using gAPI.Core.Dtos;
using gAPI.Core.Enums;
using gAPI.Core.Ids;
using gAPI.Core.Serializers;
using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Net.WebSockets;
using System.Threading.Channels;

namespace gAPI.Core.Server.Wss;

public class WssServerConnectionSender(
    WssServerConnection wssServerConnection,
    ILoggerFactory loggerFactory)
{
    private byte[] SendBuffer = new byte[wssServerConnection.Config.MaxPackageSize ?? 512 * 1024];
    readonly Channel<SendQueueItem> SendQueue = Channel.CreateUnbounded<SendQueueItem>();
    public ILogger<WssServerConnection> Logger { get; } = loggerFactory.CreateLogger<WssServerConnection>();

    public async Task SendKernel(WebSocket socket, CancellationTokenSource cts)
    {
        try
        {
            var offset = 0;
            var span = SendBuffer.AsSpan();
            var ct = cts.Token;

            // Send Id's
            var ids = new SynchronizeClientIdsDto(
                wssServerConnection.FabricManagerId,
                wssServerConnection.FabricConnectionId,
                wssServerConnection.ClientConnectionId);

            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_Ids_ToClientAsync({ids})", DateTime.Now.ToString("HH:mm:ss.fff"), ids);

            span.WriteWssServerToClientMessageEnum(ref offset, WssServerToClientMessageEnum.SynchronizeClientIds);
            span.Write(ref offset, ids);
            await socket.SendAsync(
                new ArraySegment<byte>(SendBuffer, 0, offset),
                WebSocketMessageType.Binary,
                true,
                ct);

            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} WssServerConnection {wssServerConnection.ClientConnectionId} started");

            await foreach (var item in SendQueue.Reader.ReadAllAsync(ct))
            {
                span = SendBuffer.AsSpan();

                // 🚀 direct serializen in pooled buffer
                offset = item.Write(span);

                // 🚀 direct versturen zonder kopie
                await socket.SendAsync(
                    new ArraySegment<byte>(SendBuffer, 0, offset),
                    WebSocketMessageType.Binary,
                    true,
                    ct);

                wssServerConnection.EnqueueSend(offset);
                item.Sent?.TrySetResult();
            }
        }
        catch (OperationCanceledException ex)
        {
            // Hier komt de cancel vanuit cts.Cancel() bij disconnect, gewoon negeren
            CompleteQueue(cts, ex);
        }
        catch (Exception ex)
        {
            // A failed send means this connection can no longer drain the queue.
            // Do not hide it: the connection owner must cancel the receiver and
            // dispose its subscriptions instead of accepting messages forever.
            Logger.LogError(ex, "WSS sender stopped for client {ClientConnectionId}", wssServerConnection.ClientConnectionId);
            CompleteQueue(cts, ex);
            //throw;
        }
    }
    private void CompleteQueue(CancellationTokenSource cts, Exception error)
    {
        cts.Cancel();

        SendQueue.Writer.TryComplete(error);

        while (SendQueue.Reader.TryRead(out var item))
            item.Sent?.TrySetException(error);
    }

    private async Task EnqueueAsync(Func<Span<byte>, int> write, CancellationToken ct)
    {
        if (!SendQueue.Writer.TryWrite(new SendQueueItem(write, null)))
        {
            return;
        }
    }
    private async Task EnqueueAndWaitAsync(Func<Span<byte>, int> write, CancellationToken ct)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        if (!SendQueue.Writer.TryWrite(new SendQueueItem(write, tcs)))
        {
            return;
        }

        await tcs.Task.WaitAsync(ct).ConfigureAwait(false);
    }

    private sealed record SendQueueItem(
        Func<Span<byte>, int> Write,
        TaskCompletionSource? Sent);

    public async Task Send_FabricSendAndForgetRequest_ToClientAsync(SendRequestClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_FabricSendAndForgetRequest_ToClientAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        await EnqueueAndWaitAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_FabricSendAndForgetRequest_ToClientAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            var offset = 0;
            writer.WriteWssServerToClientMessageEnum(ref offset, WssServerToClientMessageEnum.FabricSendAndForgetRequest);
            writer.Write(ref offset, message);
            return offset;
        }, ct);
    }
    public async Task Send_FabricSendAndForgetRequestCancelled_ToClientAsync(SendRequestCancelledClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_FabricSendAndForgetRequestCancelled_ToClientAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_FabricSendAndForgetRequestCancelled_ToClientAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            var offset = 0;
            writer.WriteWssServerToClientMessageEnum(ref offset, WssServerToClientMessageEnum.FabricSendAndForgetRequestCancelled);
            writer.Write(ref offset, message);
            return offset;
        }, ct);
    }

    public async Task Send_SubscribeAck_ToClientAsync(SubscribeAckDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_SubscribeAck_ToClientAsync({message})", DateTime.Now.ToString("HH:mm:ss.fff"), message);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_SubscribeAck_ToClientAsync({message})", DateTime.Now.ToString("HH:mm:ss.fff"), message);

            var offset = 0;
            writer.WriteWssServerToClientMessageEnum(ref offset, WssServerToClientMessageEnum.SubscribeAck);
            writer.Write(ref offset, message);
            return offset;
        }, ct);
    }
    public async Task Send_UnsubscribeAck_ToClientAsync(UnsubscribeAckDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_UnsubscribeAck_ToClientAsync({message})", DateTime.Now.ToString("HH:mm:ss.fff"), message);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_UnsubscribeAck_ToClientAsync({message})", DateTime.Now.ToString("HH:mm:ss.fff"), message);

            var offset = 0;
            writer.WriteWssServerToClientMessageEnum(ref offset, WssServerToClientMessageEnum.UnsubscribeAck);
            writer.Write(ref offset, message);
            return offset;
        }, ct);
    }



    public async Task Send_FabricSendRequest_ToClientAsync(SendRequestClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_FabricSendRequest_ToClientAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        await EnqueueAndWaitAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_FabricSendRequest_ToClientAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            var offset = 0;
            writer.WriteWssServerToClientMessageEnum(ref offset, WssServerToClientMessageEnum.FabricSendRequest);
            writer.Write(ref offset, message);
            return offset;
        }, ct);
    }
    public async Task Send_FabricSendRequestCancelled_ToClientAsync(SendRequestCancelledClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_FabricSendRequestCancelled_ToClientAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_FabricSendRequestCancelled_ToClientAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            var offset = 0;
            writer.WriteWssServerToClientMessageEnum(ref offset, WssServerToClientMessageEnum.FabricSendRequestCancelled);
            writer.Write(ref offset, message);
            return offset;
        }, ct);
    }
    public async Task Send_SendRequestDone_ToClientAsync(SendRequestDoneClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_SendRequestDone_ToClientAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_SendRequestDone_ToClientAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            var offset = 0;
            writer.WriteWssServerToClientMessageEnum(ref offset, WssServerToClientMessageEnum.SendRequestDone);
            writer.Write(ref offset, message);
            return offset;
        }, ct);
    }

    public async Task Send_FabricInvokeRequest_ToClientAsync(InvokeRequestClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_FabricInvokeRequest_ToClientAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        await EnqueueAndWaitAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_FabricInvokeRequest_ToClientAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            var offset = 0;
            writer.WriteWssServerToClientMessageEnum(ref offset, WssServerToClientMessageEnum.FabricInvokeRequest);
            writer.Write(ref offset, message);
            return offset;
        }, ct);
    }
    public async Task Send_FabricInvokeRequestCancelled_ToClientAsync(InvokeRequestCancelledClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_FabricInvokeCancelled_ToClientAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_FabricInvokeCancelled_ToClientAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            var offset = 0;
            writer.WriteWssServerToClientMessageEnum(ref offset, WssServerToClientMessageEnum.FabricInvokeRequestCancelled);
            writer.Write(ref offset, message);
            return offset;
        }, ct);
    }
    public async Task Send_InvokeRequestAck_ToClientAsync(InvokeRequestAckClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_InvokeRequestAck_ToClientAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_InvokeRequestAck_ToClientAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            var offset = 0;
            writer.WriteWssServerToClientMessageEnum(ref offset, WssServerToClientMessageEnum.InvokeRequestAck);
            writer.Write(ref offset, message);
            return offset;
        }, ct);
    }
    public async Task Send_InvokeRequestDone_ToClientAsync(InvokeRequestDoneClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_InvokeRequestDone_ToClientAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_InvokeRequestDone_ToClientAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            var offset = 0;
            writer.WriteWssServerToClientMessageEnum(ref offset, WssServerToClientMessageEnum.InvokeRequestDone);
            writer.Write(ref offset, message);
            return offset;
        }, ct);
    }

    public async Task Send_StreamingRequest_ToClientAsync(StreamingRequestClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_StreamingRequest_ToClientAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_StreamingRequest_ToClientAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            var offset = 0;
            writer.WriteWssServerToClientMessageEnum(ref offset, WssServerToClientMessageEnum.StreamingRequest);
            writer.Write(ref offset, message);
            return offset;
        }, ct);
    }
    public async Task Send_StreamingResponse_ToClientAsync(StreamingResponseClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_StreamingResponse_ToClientAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        await EnqueueAndWaitAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_StreamingResponse_ToClientAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            var offset = 0;
            writer.WriteWssServerToClientMessageEnum(ref offset, WssServerToClientMessageEnum.StreamingResponse);
            writer.Write(ref offset, message);
            return offset;
        }, ct);
    }

    public async Task Send_FabricStreamingRequest_ToClientAsync(StreamingRequestClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_FabricStreamingRequest_ToClientAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_FabricStreamingRequest_ToClientAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            var offset = 0;
            writer.WriteWssServerToClientMessageEnum(ref offset, WssServerToClientMessageEnum.FabricStreamingRequest);
            writer.Write(ref offset, message);
            return offset;
        }, ct);
    }
    public async Task Send_FabricStreamingResponse_ToClientAsync(StreamingResponseClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_FabricStreamingResponse_ToClientAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        await EnqueueAndWaitAsync(writer =>
        {
                if (Logger.IsEnabled(LogLevel.Trace))
                    Logger.LogTrace("{now} Send_FabricStreamingResponse_ToClientAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

                var offset = 0;
                writer.WriteWssServerToClientMessageEnum(ref offset, WssServerToClientMessageEnum.FabricStreamingResponse);
                writer.Write(ref offset, message);
                return offset;
        }, ct);
    }
}
