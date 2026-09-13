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

    public async Task Send_Initialize_ToServerAsync(InitializeDto initialize, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_SendRequest_ToServiceAsync({initialize})", DateTime.Now.ToString("HH:mm:ss.fff"), initialize);

        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.Initialize);
            writer.Write(ref offset, initialize);
            return offset;
        }, ct);
    }

    public async Task Send_Subscribe_ToServerAsync(SubscribeDto subscribe, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_Subscribe_ToServerAsync({subscribe})", DateTime.Now.ToString("HH:mm:ss.fff"), subscribe);

        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.Subscribe);
            writer.Write(ref offset, subscribe);
            return offset;
        }, ct);
    }
    public async Task Send_Unsubscribe_ToServerAsync(UnsubscribeDto unsubscribe, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_Unsubscribe_ToServerAsync({unsubscribe})", DateTime.Now.ToString("HH:mm:ss.fff"), unsubscribe);

        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.Unsubscribe);
            writer.Write(ref offset, unsubscribe);
            return offset;
        }, ct);
    }

    public async Task Send_SendRequest_ToServerAsync(SendRequestClientDto sendRequest, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_SendRequest_ToServerAsync({sendRequest})", DateTime.Now.ToString("HH:mm:ss.fff"), sendRequest);

        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.SendRequest);
            writer.Write(ref offset, sendRequest);
            return offset;
        }, ct);
    }
    public async Task Send_SendRequestCancelled_ToServerAsync(SendRequestCancelledClientDto sendRequestCancelled, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_SendRequestCancelled_ToServerAsync({sendRequestCancelled})", DateTime.Now.ToString("HH:mm:ss.fff"), sendRequestCancelled);

        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.SendRequestCancelled);
            writer.Write(ref offset, sendRequestCancelled);
            return offset;
        }, ct);
    }
    public async Task Send_SendRequestDone_ToServerAsync(SendRequestDoneClientDto sendRequestDone, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_SendRequestDone_ToServerAsync({sendRequestDone})", DateTime.Now.ToString("HH:mm:ss.fff"), sendRequestDone);

        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.FabricSendRequestDone);
            writer.Write(ref offset, sendRequestDone);
            return offset;
        }, ct);
    }
    
    public async Task Send_InvokeRequest_ToServerAsync(InvokeRequestClientDto invokeRequest, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_InvokeRequest_ToServerAsync({invokeRequest})", DateTime.Now.ToString("HH:mm:ss.fff"), invokeRequest);

        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.InvokeRequest);
            writer.Write(ref offset, invokeRequest);
            return offset;
        }, ct);
    }
    public async Task Send_InvokeRequestCancelled_ToServerAsync(InvokeRequestCancelledClientDto invokeRequestCancelled, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_InvokeCancelled_ToServerAsync({invokeRequestCancelled})", DateTime.Now.ToString("HH:mm:ss.fff"), invokeRequestCancelled);

        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.InvokeRequestCancelled);
            writer.Write(ref offset, invokeRequestCancelled);
            return offset;
        }, ct);
    }
    public async Task Send_InvokeRequestDone_ToServerAsync(InvokeRequestDoneClientDto invokeRequestDone, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_InvokeRequestDone_ToServerAsync({invokeRequestDone})", DateTime.Now.ToString("HH:mm:ss.fff"), invokeRequestDone);

        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.FabricInvokeRequestDone);
            writer.Write(ref offset, invokeRequestDone);
            return offset;
        }, ct);
    }

    public async Task Send_StreamingRequest_ToServerAsync(StreamingRequestClientDto request, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_StreamingRequest_ToServerAsync({request})", DateTime.Now.ToString("HH:mm:ss.fff"), request);

        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.StreamingRequest);
            writer.Write(ref offset, request);
            return offset;
        }, ct);
    }
    public async Task Send_StreamingResponse_ToServerAsync(StreamingResponseClientDto response, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_StreamingResponse_ToServerAsync({response})", DateTime.Now.ToString("HH:mm:ss.fff"), response);

        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.StreamingResponse);
            writer.Write(ref offset, response);
            return offset;
        }, ct);
    }

    public async Task Send_FabricStreamingRequest_ToServerAsync(StreamingRequestClientDto request, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_FabricStreamingRequest_ToServerAsync({request})", DateTime.Now.ToString("HH:mm:ss.fff"), request);

        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.FabricStreamingRequest);
            writer.Write(ref offset, request);
            return offset;
        }, ct);
    }
    public async Task Send_FabricStreamingResponse_ToServerAsync(StreamingResponseClientDto response, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_FabricStreamingResponse_ToServerAsync({response})", DateTime.Now.ToString("HH:mm:ss.fff"), response);

        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssClientToServerMessageEnum(ref offset, WssClientToServerMessageEnum.FabricStreamingResponse);
            writer.Write(ref offset, response);
            return offset;
        }, ct);
    }

    public async Task Send_Log_ToServerAsync(WssLoggerLogDto log, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Send_Log_ToServerAsync({log})", log);

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