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
    private Channel<SendQueueItem>? SendQueue;
    public bool Ready => SendQueue != null;

    public async Task SendKernel(BinaryWriter writer, CancellationToken ct)
    {
        try
        {
            var sendQueue = Channel.CreateUnbounded<SendQueueItem>();
            SendQueue = sendQueue;

            await foreach (var item in sendQueue.Reader.ReadAllAsync(ct))
            {
                try
                {
                    item.Write(writer);
                    writer.Flush();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "SendKernel");
                    item.Sent?.TrySetException(ex);
                    throw;
                }

                item.Sent?.TrySetResult();
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
            Logger.LogError(ex, "WSS sender stopped for client {FabricConnectionId}", fabricClient.FabricConnectionId);
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

    private async Task EnqueueAsync(Action<BinaryWriter> write, CancellationToken ct)
    {
        if (SendQueue?.Writer.TryWrite(new SendQueueItem(write, null)) != true)
            throw new Exception("Not connected");
    }
    private async Task EnqueueAndWaitAsync(Action<BinaryWriter> write, CancellationToken ct)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        if (SendQueue?.Writer.TryWrite(new SendQueueItem(write, completion)) != true)
            throw new Exception("Not connected");

        await completion.Task.WaitAsync(ct).ConfigureAwait(false);
    }

    public async Task Send_UpdateSession_ToFabricAsync(UpdateSessionDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_UpdateSession_ToFabricAsync({message})", DateTime.Now.ToString("HH:mm:ss.fff"), message);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_UpdateSession_ToFabricAsync({message})", DateTime.Now.ToString("HH:mm:ss.fff"), message);

            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.UpdateSession);
            writer.Write(message);
        }, ct);
    }
    public async Task Send_ClearSession_ToFabricAsync(SendClearSessionDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_ClearSession_ToFabricAsync({message})", DateTime.Now.ToString("HH:mm:ss.fff"), message);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_ClearSession_ToFabricAsync({message})", DateTime.Now.ToString("HH:mm:ss.fff"), message);

            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.ClearSession);
            writer.Write(message);
        }, ct);
    }
    public async Task Send_GetSession_ToFabricAsync(SendGetSessionCookieDataDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_GetSession_ToFabricAsync({message})", DateTime.Now.ToString("HH:mm:ss.fff"), message);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_GetSession_ToFabricAsync({message})", DateTime.Now.ToString("HH:mm:ss.fff"), message);

            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.GetSessionCookieData);
            writer.Write(message);
        }, ct);
    }

    public async Task Send_Subscribe_ToFabricAsync(SubscribeDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_Subscribe_ToFabricAsync({message})", DateTime.Now.ToString("HH:mm:ss.fff"), message);

        await EnqueueAsync(w =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_Subscribe_ToFabricAsync({message})", DateTime.Now.ToString("HH:mm:ss.fff"), message);

            FabricConverter.WriteClientToHostMessageType(w, FabricClientToHostMessageEnum.Subscribe);
            w.Write(message);
        }, ct);
    }
    public async Task Send_Unsubscribe_ToFabricAsync(UnsubscribeDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_Unsubscribe_ToFabricAsync({message})", DateTime.Now.ToString("HH:mm:ss.fff"), message);

        await EnqueueAsync(w =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_Unsubscribe_ToFabricAsync({message})", DateTime.Now.ToString("HH:mm:ss.fff"), message);

            FabricConverter.WriteClientToHostMessageType(w, FabricClientToHostMessageEnum.Unsubscribe);
            w.Write(message);
        }, ct);
    }

    public async Task Send_SendAndForgetRequest_ToFabricAsync(SendRequestDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_SendAndForgetRequest_ToFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        await EnqueueAndWaitAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_SendAndForgetRequest_ToFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.SendAndForgetRequest);
            writer.Write(message);
        }, ct);
    }

    public async Task Send_SendRequest_ToFabricAsync(SendRequestDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_SendRequest_ToFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        await EnqueueAndWaitAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_SendRequest_ToFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.SendRequest);
            writer.Write(message);
        }, ct);
    }
    public async Task Send_SendRequestCancelled_ToFabricAsync(SendRequestCancelledDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_SendRequestCancelled_ToFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_SendRequestCancelled_ToFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.SendRequestCancelled);
            writer.Write(message);
        }, ct);
    }
    public async Task Send_SendRequestDone_ToFabricAsync(SendRequestDoneDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_SendRequestDone_ToFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_SendRequestDone_ToFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.SendRequestDone);
            writer.Write(message);
        }, ct);
    }

    public async Task Send_InvokeRequest_ToFabricAsync(InvokeRequestDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_InvokeRequest_ToFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        await EnqueueAndWaitAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_InvokeRequest_ToFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.InvokeRequest);
            writer.Write(message);
        }, ct);
    }
    public async Task Send_InvokeRequestCancelled_ToFabricAsync(InvokeRequestCancelledDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_InvokeRequestCancelled_ToFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_InvokeRequestCancelled_ToFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.InvokeRequestCancelled);
            writer.Write(message);
        }, ct);
    }
    public async Task Send_InvokeRequestAck_ToFabricAsync(InvokeRequestAckDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_InvokeRequestAck_ToFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_InvokeRequestAck_ToFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.InvokeRequestAck);
            writer.Write(message);
        }, ct);
    }
    public async Task Send_InvokeRequestDone_ToFabricAsync(InvokeRequestDoneDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_InvokeRequestDone_ToFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_InvokeRequestDone_ToFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.InvokeRequestDone);
            writer.Write(message);
        }, ct);
    }

    public async Task Send_StreamingRequest_ToFabricAsync(StreamingRequestDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_StreamingRequest_ToFabricAsync({RequestId}, {ServiceId}, {MethodId}, {index})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId, message.ArgumentIndex);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_StreamingRequest_ToFabricAsync({RequestId}, {ServiceId}, {MethodId}, {index})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId, message.ArgumentIndex);

            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.StreamingRequest);
            writer.Write(message);
        }, ct);
    }
    public async Task Send_StreamingResponse_ToFabricAsync(StreamingResponseDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_StreamingResponse_ToFabricAsync({RequestId}, {ServiceId}, {MethodId}, {index})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId, message.ArgumentIndex);

        await EnqueueAndWaitAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_StreamingResponse_ToFabricAsync({RequestId}, {ServiceId}, {MethodId}, {index})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId, message.ArgumentIndex);

            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.StreamingResponse);
            writer.Write(message);
        }, ct);
    }

    public async Task Send_Log_ToFabricAsync(LogDto message, CancellationToken ct)
    {
        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.Log);
            writer.Write(message);
        }, ct);
    }

    record SendQueueItem(
        Action<BinaryWriter> Write,
        TaskCompletionSource? Sent);
}