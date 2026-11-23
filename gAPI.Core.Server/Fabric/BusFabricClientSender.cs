using gAPI.Core.Dtos;
using gAPI.Core.Ids;
using gAPI.Core.Server.Config;
using gAPI.Core.Server.Dtos;
using gAPI.Core.Server.Enums;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace gAPI.Core.Server.Fabric;

public class BusFabricClientSender(
    BusFabricClient fabricClient,
    ILoggerFactory loggerFactory)
{
    private readonly ILogger Logger = loggerFactory.CreateLogger<FabricClientSender>();
    private Channel<SendQueueItem>? SendQueue;

    public async Task SendKernel(BinaryWriter writer, CancellationToken ct)
    {
        try
        {
            var sendQueue = Channel.CreateUnbounded<SendQueueItem>();
            SendQueue = sendQueue;

            await foreach (var item in SendQueue.Reader.ReadAllAsync(ct))
            {
                try
                {
                    item.Write(writer);
                    writer.Flush();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error writing to Fabric stream");
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
        var sendQueue = SendQueue;
        while (sendQueue == null)
        {
            await Task.Delay(100);
            sendQueue = SendQueue;
        }

        await sendQueue.Writer.WriteAsync(new SendQueueItem(write, null), ct);
    }
    private async Task EnqueueAndWaitAsync(Action<BinaryWriter> write, CancellationToken ct)
    {
        var sendQueue = SendQueue;
        while (sendQueue == null)
        {
            await Task.Delay(100);
            sendQueue = SendQueue;
        }

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await sendQueue.Writer.WriteAsync(new SendQueueItem(write, completion), ct);

        await completion.Task.WaitAsync(ct).ConfigureAwait(false);
    }

    public async Task Send_BusSubscribe_ToFabricAsync(BusSubscribeDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_BusSubscribe_ToFabricAsync({message})", DateTime.Now.ToString("HH:mm:ss.fff"), message);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_BusSubscribe_ToFabricAsync({message})", DateTime.Now.ToString("HH:mm:ss.fff"), message);

            BusFabricConverter.WriteClientToHostMessageType(writer, BusFabricClientToHostMessageEnum.BusSubscribe);
            writer.Write(message);
        }, ct);
    }
    public async Task Send_BusUnsubscribe_ToFabricAsync(BusUnsubscribeDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_BusUnsubscribe_ToFabricAsync({message})", DateTime.Now.ToString("HH:mm:ss.fff"), message);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_BusUnsubscribe_ToFabricAsync({message})", DateTime.Now.ToString("HH:mm:ss.fff"), message);

            BusFabricConverter.WriteClientToHostMessageType(writer, BusFabricClientToHostMessageEnum.BusUnsubscribe);
            writer.Write(message);
        }, ct);
    }

    public async Task Send_BusSendAndForgetRequest_ToFabricAsync(BusSendRequestDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_BusSendAndForgetRequest_ToFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        await EnqueueAndWaitAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_BusSendAndForgetRequest_ToFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

            BusFabricConverter.WriteClientToHostMessageType(writer, BusFabricClientToHostMessageEnum.BusFireAndForget);
            writer.Write(message);
        }, ct);
    }

    public async Task Send_BusSendRequest_ToFabricAsync(BusSendRequestDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_BusSendRequest_ToFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        await EnqueueAndWaitAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_BusSendRequest_ToFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

            BusFabricConverter.WriteClientToHostMessageType(writer, BusFabricClientToHostMessageEnum.BusSendRequest);
            writer.Write(message);
        }, ct);
    }
    public async Task Send_BusSendCancelled_ToFabricAsync(BusSendRequestCancelledDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_FabricStreSend_BusSubscribe_ToFabricAsyncamingRequest_ToServerAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_BusSubscribe_ToFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

            BusFabricConverter.WriteClientToHostMessageType(writer, BusFabricClientToHostMessageEnum.BusSendRequestCancelled);
            writer.Write(message);
        }, ct);
    }
    public async Task Send_BusSendRequestDone_ToFabricAsync(BusSendRequestDoneDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_BusSendRequestDone_ToFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_BusSendRequestDone_ToFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

            BusFabricConverter.WriteClientToHostMessageType(writer, BusFabricClientToHostMessageEnum.BusSendRequestDone);
            writer.Write(message);
        }, ct);
    }

    public async Task Send_BusInvokeRequest_ToFabricAsync(BusInvokeRequestDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_BusInvokeRequest_ToFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        await EnqueueAndWaitAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_BusInvokeRequest_ToFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

            BusFabricConverter.WriteClientToHostMessageType(writer, BusFabricClientToHostMessageEnum.BusInvokeRequest);
            writer.Write(message);
        }, ct);
    }
    public async Task Send_BusInvokeRequestCancelled_ToFabricAsync(BusInvokeRequestCancelledDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_BusInvokeRequestCancelled_ToFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_BusInvokeRequestCancelled_ToFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

            BusFabricConverter.WriteClientToHostMessageType(writer, BusFabricClientToHostMessageEnum.BusInvokeRequestCancelled);
            writer.Write(message);
        }, ct);
    }
    public async Task Send_BusInvokeRequestAck_ToFabricAsync(BusInvokeRequestAckDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_BusInvokeRequestAck_ToFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_BusInvokeRequestAck_ToFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

            BusFabricConverter.WriteClientToHostMessageType(writer, BusFabricClientToHostMessageEnum.BusInvokeRequestAck);
            writer.Write(message);
        }, ct);
    }
    public async Task Send_BusInvokeRequestDone_ToFabricAsync(BusInvokeRequestDoneDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_BusInvokeRequestDone_ToFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_BusInvokeRequestDone_ToFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

            BusFabricConverter.WriteClientToHostMessageType(writer, BusFabricClientToHostMessageEnum.BusInvokeRequestDone);
            writer.Write(message);
        }, ct);
    }

    public async Task Send_BusStreamingRequest_ToFabricAsync(BusStreamingRequestDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_BusStreamingRequest_ToFabricAsync({RequestId}, {ServiceId}, {MethodId}, {index})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId, message.ArgumentIndex);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_BusStreamingRequest_ToFabricAsync({RequestId}, {ServiceId}, {MethodId}, {index})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId, message.ArgumentIndex);

            BusFabricConverter.WriteClientToHostMessageType(writer, BusFabricClientToHostMessageEnum.BusStreamingRequest);
            writer.Write(message);
        }, ct);
    }
    public async Task Send_BusStreamingResponse_ToFabricAsync(BusStreamingResponseDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_BusStreamingResponse_ToFabricAsync({RequestId}, {ServiceId}, {MethodId}, {index})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId, message.ArgumentIndex);

        await EnqueueAndWaitAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_BusStreamingResponse_ToFabricAsync({RequestId}, {ServiceId}, {MethodId}, {index})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId, message.ArgumentIndex);

            BusFabricConverter.WriteClientToHostMessageType(writer, BusFabricClientToHostMessageEnum.BusStreamingResponse);
            writer.Write(message);
        }, ct);
    }

    record SendQueueItem(
        Action<BinaryWriter> Write,
        TaskCompletionSource? Sent);
}