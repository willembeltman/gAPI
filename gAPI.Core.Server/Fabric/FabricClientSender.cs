using gAPI.Core.Dtos;
using gAPI.Core.Ids;
using gAPI.Core.Server.Enums;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace gAPI.Core.Server.Fabric;

public class FabricClientSender(
    FabricClient fabricClient,
    ILoggerFactory loggerFactory)
{
    private readonly ILogger Logger = loggerFactory.CreateLogger<FabricClientSender>();
    readonly Channel<SendQueueItem> SendQueue = Channel.CreateUnbounded<SendQueueItem>();

    public async Task SendKernel(CancellationToken ct)
    {
        try
        {
            await foreach (var item in SendQueue.Reader.ReadAllAsync(ct))
            {
                var send = false;
                while (send == false)
                {
                    try
                    {
                        if (fabricClient.IsConnected && fabricClient.Writer != null)
                        {
                            item.Write(fabricClient.Writer);
                            fabricClient.Writer.Flush();
                            send = true;
                        }
                        else
                        {
                            await Task.Delay(10);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Error writing to Fabric stream");
                    }
                }

                item.Sent?.TrySetResult();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "WSS sender stopped for client {FabricConnectionId}", fabricClient.FabricConnectionId);
            CompleteQueue(ex);
        }
    }
    private void CompleteQueue(Exception error)
    {
        SendQueue.Writer.TryComplete(error);

        while (SendQueue.Reader.TryRead(out var item))
            item.Sent?.TrySetException(error);
    }

    private async Task EnqueueAsync(Action<BinaryWriter> write, CancellationToken ct)
    {
        try
        {
            await SendQueue.Writer.WriteAsync(new SendQueueItem(write, null), ct);
        }
        catch (TaskCanceledException ex)
        {
            Logger.LogError(ex, "EnqueueAsync");
        }
    }
    private async Task EnqueueAndWaitAsync(Action<BinaryWriter> write, CancellationToken ct)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        try
        {
            await SendQueue.Writer.WriteAsync(new SendQueueItem(write, tcs), ct);
        }
        catch (TaskCanceledException ex)
        {
            Logger.LogError(ex, "EnqueueAsync");
        }

        await tcs.Task.WaitAsync(ct).ConfigureAwait(false);
    }

    public async Task Send_UpdateSession_ToFabricAsync(UpdateSessionDto updateSessionDto, CancellationToken ct)
    {
        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.UpdateSession);
            writer.Write(updateSessionDto);
        }, ct);
    }
    public async Task Send_ClearSession_ToFabricAsync(SendClearSessionDto clearSessionDto, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_ClearSession_ToFabricAsync({clearSessionDto})", DateTime.Now.ToString("HH:mm:ss.fff"), clearSessionDto);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_ClearSession_ToFabricAsync({clearSessionDto})", DateTime.Now.ToString("HH:mm:ss.fff"), clearSessionDto);

            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.ClearSession);
            writer.Write(clearSessionDto);
        }, ct);
    }
    public async Task Send_GetSession_ToFabricAsync(SendGetSessionCookieDataDto getSessionDto, CancellationToken ct)
    {
        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.GetSessionCookieData);
            writer.Write(getSessionDto);
        }, ct);
    }

    public async Task Send_Subscribe_ToFabricAsync(SubscribeDto message, CancellationToken ct)
    {
        await EnqueueAsync(w =>
        {
            FabricConverter.WriteClientToHostMessageType(w, FabricClientToHostMessageEnum.Subscribe);
            w.Write(message);
        }, ct);
    }
    public async Task Send_Unsubscribe_ToFabricAsync(UnsubscribeDto message, CancellationToken ct)
    {
        await EnqueueAsync(w =>
        {
            FabricConverter.WriteClientToHostMessageType(w, FabricClientToHostMessageEnum.Unsubscribe);
            w.Write(message);
        }, ct);
    }

    public async Task Send_SendAndForgetRequest_ToFabricAsync(SendRequestDto message, CancellationToken ct)
    {
        await EnqueueAndWaitAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.SendAndForgetRequest);
            writer.Write(message);
        }, ct);
    }
    public async Task Send_SendAndForgetRequestCancelled_ToFabricAsync(SendRequestCancelledDto message, CancellationToken ct)
    {
        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.SendAndForgetRequestCancelled);
            writer.Write(message);
        }, ct);
    }

    public async Task Send_SendRequest_ToFabricAsync(SendRequestDto message, CancellationToken ct)
    {
        await EnqueueAndWaitAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.SendRequest);
            writer.Write(message);
        }, ct);
    }
    public async Task Send_SendRequestCancelled_ToFabricAsync(SendRequestCancelledDto message, CancellationToken ct)
    {
        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.SendRequestCancelled);
            writer.Write(message);
        }, ct);
    }
    public async Task Send_SendRequestDone_ToFabricAsync(SendRequestDoneDto message, CancellationToken ct)
    {
        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.SendRequestDone);
            writer.Write(message);
        }, ct);
    }

    public async Task Send_InvokeRequest_ToFabricAsync(InvokeRequestDto message, CancellationToken ct)
    {
        await EnqueueAndWaitAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.InvokeRequest);
            writer.Write(message);
        }, ct);
    }
    public async Task Send_InvokeRequestCancelled_ToFabricAsync(InvokeRequestCancelledDto message, CancellationToken ct)
    {
        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.InvokeRequestCancelled);
            writer.Write(message);
        }, ct);
    }
    public async Task Send_InvokeRequestAck_ToFabricAsync(InvokeRequestAckDto message, CancellationToken ct)
    {
        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.InvokeRequestAck);
            writer.Write(message);
        }, ct);
    }
    public async Task Send_InvokeRequestDone_ToFabricAsync(InvokeRequestDoneDto message, CancellationToken ct)
    {
        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.InvokeRequestDone);
            writer.Write(message);
        }, ct);
    }

    public async Task Send_StreamingRequest_ToFabricAsync(StreamingRequestDto message, CancellationToken ct)
    {
        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.StreamingRequest);
            writer.Write(message);
        }, ct);
    }
    public async Task Send_StreamingResponse_ToFabricAsync(StreamingResponseDto message, CancellationToken ct)
    {
        await EnqueueAndWaitAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.StreamingResponse);
            writer.Write(message);
        }, ct);
    }

    record SendQueueItem(
        Action<BinaryWriter> Write,
        TaskCompletionSource? Sent);
}