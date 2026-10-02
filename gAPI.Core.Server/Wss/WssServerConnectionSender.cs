using gAPI.Core.Dtos;
using gAPI.Core.Enums;
using gAPI.Core.Ids;
using gAPI.Core.Serializers;
using gAPI.Core.Wss;
using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Net.WebSockets;
using System.Threading.Channels;

namespace gAPI.Core.Server.Wss;

public class WssServerConnectionSender(
    WssServerConnection wssServerConnection,
    ILoggerFactory loggerFactory)
{
    private byte[] SendBuffer = new byte[10 * 1024 * 1024];
    readonly Channel<Func<Span<byte>, int>> SendQueue = Channel.CreateUnbounded<Func<Span<byte>, int>>();
    public ILogger<WssServerConnection> Logger { get; } = loggerFactory.CreateLogger<WssServerConnection>();

    public async Task SendKernel(WebSocket socket, CancellationToken ct)
    {
        try
        {
            await Send_Ids_ToClientAsync(socket, ct);

            await foreach (var item in SendQueue.Reader.ReadAllAsync(ct))
            {
                var span = SendBuffer.AsSpan();

                // 🚀 direct serializen in pooled buffer
                var offset = item(span);

                // 🚀 direct versturen zonder kopie
                await socket.SendAsync(
                    new ArraySegment<byte>(SendBuffer, 0, offset),
                    WebSocketMessageType.Binary,
                    true,
                    ct);

                wssServerConnection.EnqueueSend(offset);
            }
        }
        catch (OperationCanceledException)
        {
            // Hier komt de cancel vanuit cts.Cancel() bij disconnect, gewoon negeren
        }
    }
    private async Task EnqueueAsync(Func<Span<byte>, int> write, CancellationToken ct)
    {
        try
        {
            await SendQueue.Writer.WriteAsync(write, ct);
        }
        catch (TaskCanceledException)
        {
        }
    }

    private async Task Send_Ids_ToClientAsync(WebSocket socket, CancellationToken ct)
    {
        var offset = 0;
        var span = SendBuffer.AsSpan();

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

    }

    public async Task Send_FabricSendRequest_ToClientAsync(SendRequestClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_FabricSendRequest_ToClientAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        await EnqueueAsync(writer =>
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

        await EnqueueAsync(writer =>
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

        await EnqueueAsync(writer =>
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

        await EnqueueAsync(writer =>
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
