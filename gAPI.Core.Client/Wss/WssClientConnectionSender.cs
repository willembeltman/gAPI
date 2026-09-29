using gAPI.Core.Client.Interfaces;
using gAPI.Core.Dtos;
using gAPI.Core.Enums;
using gAPI.Core.Helpers;
using gAPI.Core.Ids;
using gAPI.Core.Interfaces;
using gAPI.Core.Serializers;
using gAPI.Core.Wss;
using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using System.Xml.Linq;

namespace gAPI.Core.Client.Wss;

public class WssClientConnectionSender(IClientLoggerFactory LoggerFactory)
{
    private readonly ILogger<WssClientConnectionSender> Logger = LoggerFactory.CreateLogger<WssClientConnectionSender>();
    private readonly byte[] SendBuffer = new byte[10 * 1024 * 1024];
    private readonly Channel<Func<Span<byte>, int>> SendQueue = Channel.CreateUnbounded<Func<Span<byte>, int>>();

    public async Task SendKernel(WebSocket socket, CancellationToken ct)
    {
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
        }
    }

    public async Task Send_Initialize_ToServerAsync(InitializeDto message, string sessionId, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_SendRequest_ToServiceAsync({sessionId})", DateTime.Now.ToString("HH:mm:ss.fff"), sessionId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_SendRequest_ToServiceAsync({sessionId})", DateTime.Now.ToString("HH:mm:ss.fff"), sessionId);

            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.Initialize);
            writer.Write(ref offset, message);
            return offset;
        }, ct);
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

    public async Task Send_SendRequest_ToServerAsync(SendRequestClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_SendRequest_ToServerAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_SendRequest_ToServerAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.SendRequest);
            writer.Write(ref offset, message);
            return offset;
        }, ct);
    }
    public async Task Send_SendRequestCancelled_ToServerAsync(SendRequestCancelledClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_SendRequestCancelled_ToServerAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_SendRequestCancelled_ToServerAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.SendRequestCancelled);
            writer.Write(ref offset, message);
            return offset;
        }, ct);
    }
    public async Task Send_SendRequestDone_ToServerAsync(SendRequestDoneClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_SendRequestDone_ToServerAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_SendRequestDone_ToServerAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.FabricSendRequestDone);
            writer.Write(ref offset, message);
            return offset;
        }, ct);
    }
    
    public async Task Send_InvokeRequest_ToServerAsync(InvokeRequestClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_InvokeRequest_ToServerAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_InvokeRequest_ToServerAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.InvokeRequest);
            writer.Write(ref offset, message);
            return offset;
        }, ct);
    }
    public async Task Send_InvokeRequestCancelled_ToServerAsync(InvokeRequestCancelledClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_InvokeCancelled_ToServerAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_InvokeCancelled_ToServerAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.InvokeRequestCancelled);
            writer.Write(ref offset, message);
            return offset;
        }, ct);
    }
    public async Task Send_InvokeRequestDone_ToServerAsync(InvokeRequestDoneClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_InvokeRequestDone_ToServerAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_InvokeRequestDone_ToServerAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.FabricInvokeRequestDone);
            writer.Write(ref offset, message);
            return offset;
        }, ct);
    }

    public async Task Send_StreamingRequest_ToServerAsync(StreamingRequestClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_StreamingRequest_ToServerAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_StreamingRequest_ToServerAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.StreamingRequest);
            writer.Write(ref offset, message);
            return offset;
        }, ct);
    }
    public async Task Send_StreamingResponse_ToServerAsync(StreamingResponseClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_StreamingResponse_ToServerAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_StreamingResponse_ToServerAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.StreamingResponse);
            writer.Write(ref offset, message);
            return offset;
        }, ct);
    }

    public async Task Send_FabricStreamingRequest_ToServerAsync(StreamingRequestClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_FabricStreamingRequest_ToServerAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_FabricStreamingRequest_ToServerAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.FabricStreamingRequest);
            writer.Write(ref offset, message);
            return offset;
        }, ct);
    }
    public async Task Send_FabricStreamingResponse_ToServerAsync(StreamingResponseClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: QUEUED Send_FabricStreamingResponse_ToServerAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now}: Send_FabricStreamingResponse_ToServerAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.FabricStreamingResponse);
            writer.Write(ref offset, message);
            return offset;
        }, ct);
    }

    public async Task Send_Log_ToServerAsync(WssLoggerLogDto log, CancellationToken ct)
    {
        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.Log);
            writer.Write(ref offset, log);
            return offset;
        }, ct);
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
}