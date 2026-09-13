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
    }

    public async Task Send_FabricSendRequest_ToClientAsync(SendRequestClientDto sendRequest, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Send_FabricSendRequest_ToClientAsync({sendRequest})", DateTime.Now.ToString("HH:mm:ss.fff"), sendRequest);

        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssServerToClientMessageEnum(ref offset, WssServerToClientMessageEnum.FabricSendRequest);
            writer.Write(ref offset, sendRequest);
            return offset;
        }, ct);
    }
    public async Task Send_FabricSendRequestCancelled_ToClientAsync(SendRequestCancelledClientDto sendRequestCancelled, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Send_FabricSendRequestCancelled_ToClientAsync({sendRequestCancelled})", DateTime.Now.ToString("HH:mm:ss.fff"), sendRequestCancelled);

        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssServerToClientMessageEnum(ref offset, WssServerToClientMessageEnum.FabricSendRequestCancelled);
            writer.Write(ref offset, sendRequestCancelled);
            return offset;
        }, ct);
    }
    public async Task Send_SendRequestDone_ToClientAsync(SendRequestDoneClientDto sendRequestDone, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Send_SendRequestDone_ToClientAsync({sendRequestDone})", DateTime.Now.ToString("HH:mm:ss.fff"), sendRequestDone);

        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssServerToClientMessageEnum(ref offset, WssServerToClientMessageEnum.SendRequestDone);
            writer.Write(ref offset, sendRequestDone);
            return offset;
        }, ct);
    }

    public async Task Send_FabricInvokeRequest_ToClientAsync(InvokeRequestClientDto invokeRequest, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Send_FabricInvokeRequest_ToClientAsync({invokeRequest})", DateTime.Now.ToString("HH:mm:ss.fff"), invokeRequest);

        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssServerToClientMessageEnum(ref offset, WssServerToClientMessageEnum.FabricInvokeRequest);
            writer.Write(ref offset, invokeRequest);
            return offset;
        }, ct);
    }
    public async Task Send_FabricInvokeRequestCancelled_ToClientAsync(InvokeRequestCancelledClientDto invokeRequestCancelledDto, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Send_FabricInvokeCancelled_ToClientAsync({invokeRequestCancelledDto})", DateTime.Now.ToString("HH:mm:ss.fff"), invokeRequestCancelledDto);

        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssServerToClientMessageEnum(ref offset, WssServerToClientMessageEnum.FabricInvokeRequestCancelled);
            writer.Write(ref offset, invokeRequestCancelledDto);
            return offset;
        }, ct);
    }
    public async Task Send_InvokeRequestDone_ToClientAsync(InvokeRequestDoneClientDto invokeResponseDoneDto, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Send_InvokeRequestDone_ToClientAsync({invokeResponseDoneDto})", DateTime.Now.ToString("HH:mm:ss.fff"), invokeResponseDoneDto);

        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssServerToClientMessageEnum(ref offset, WssServerToClientMessageEnum.InvokeRequestDone);
            writer.Write(ref offset, invokeResponseDoneDto);
            return offset;
        }, ct);
    }

    public async Task Send_StreamingRequest_ToClientAsync(StreamingRequestClientDto request, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Send_StreamingRequest_ToClientAsync({request})", DateTime.Now.ToString("HH:mm:ss.fff"), request);

        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssServerToClientMessageEnum(ref offset, WssServerToClientMessageEnum.StreamingRequest);
            writer.Write(ref offset, request);
            return offset;
        }, ct);
    }

    public async Task Send_StreamingResponse_ToClientAsync(StreamingResponseClientDto response, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Send_StreamingResponse_ToClientAsync({response})", DateTime.Now.ToString("HH:mm:ss.fff"), response);

        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssServerToClientMessageEnum(ref offset, WssServerToClientMessageEnum.StreamingResponse);
            writer.Write(ref offset, response);
            return offset;
        }, ct);
    }

    public async Task Send_FabricStreamingRequest_ToClientAsync(StreamingRequestClientDto request, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Send_FabricStreamingRequest_ToClientAsync({request})", DateTime.Now.ToString("HH:mm:ss.fff"), request);

        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssServerToClientMessageEnum(ref offset, WssServerToClientMessageEnum.FabricStreamingRequest);
            writer.Write(ref offset, request);
            return offset;
        }, ct);
    }
    public async Task Send_FabricStreamingResponse_ToClientAsync(StreamingResponseClientDto response, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Send_FabricStreamingResponse_ToClientAsync({response})", DateTime.Now.ToString("HH:mm:ss.fff"), response);

        await EnqueueAsync(writer =>
        {
            var offset = 0;
            writer.WriteWssServerToClientMessageEnum(ref offset, WssServerToClientMessageEnum.FabricStreamingResponse);
            writer.Write(ref offset, response);
            return offset;
        }, ct);
    }
}
