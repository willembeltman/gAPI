using gAPI.Core.Dtos;
using gAPI.Core.Ids;
using gAPI.Core.Server.Dtos;
using gAPI.Core.Server.Enums;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace gAPI.Core.Server.Fabric;

public class PlaneFabricClientSender(
    PlaneFabricClient fabricClient,
    ILoggerFactory loggerFactory)
{
    private readonly ILogger Logger = loggerFactory.CreateLogger<FabricClientSender>();
    private Channel<SendQueueItem>? SendQueue;

    public bool Ready => SendQueue != null;

    public async Task SendKernel(BinaryWriter writer, CancellationToken ct)
    {
        try
        {
            SendQueue = Channel.CreateUnbounded<SendQueueItem>();
            await foreach (var item in SendQueue.Reader.ReadAllAsync(ct))
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
            Logger.LogError(ex, "Plane sender stopped for client {FabricConnectionId}", fabricClient.FabricConnectionId);
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

    public async Task Send_PlaneSubscribe_ToFabricAsync(PlaneSubscribeDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_PlaneSubscribe_ToFabricAsync({message})", DateTime.Now.ToString("HH:mm:ss.fff"), message);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_PlaneSubscribe_ToFabricAsync({message})", DateTime.Now.ToString("HH:mm:ss.fff"), message);

            PlaneFabricConverter.WriteClientToHostMessageType(writer, PlaneFabricClientToHostMessageEnum.PlaneSubscribe);
            writer.Write(message);
        }, ct);
    }
    public async Task Send_PlaneUnsubscribe_ToFabricAsync(PlaneUnsubscribeDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_PlaneUnsubscribe_ToFabricAsync({message})", DateTime.Now.ToString("HH:mm:ss.fff"), message);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_PlaneUnsubscribe_ToFabricAsync({message})", DateTime.Now.ToString("HH:mm:ss.fff"), message);

            PlaneFabricConverter.WriteClientToHostMessageType(writer, PlaneFabricClientToHostMessageEnum.PlaneUnsubscribe);
            writer.Write(message);
        }, ct);
    }

    public async Task Send_PlaneSendAndForgetRequest_ToFabricAsync(PlaneSendRequestDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_PlaneSendAndForgetRequest_ToFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        await EnqueueAndWaitAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_PlaneSendAndForgetRequest_ToFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

            PlaneFabricConverter.WriteClientToHostMessageType(writer, PlaneFabricClientToHostMessageEnum.PlaneFireAndForget);
            writer.Write(message);
        }, ct);
    }

    public async Task Send_PlaneSendRequest_ToFabricAsync(PlaneSendRequestDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_PlaneSendRequest_ToFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        await EnqueueAndWaitAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_PlaneSendRequest_ToFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

            PlaneFabricConverter.WriteClientToHostMessageType(writer, PlaneFabricClientToHostMessageEnum.PlaneSendRequest);
            writer.Write(message);
        }, ct);
    }
    public async Task Send_PlaneSendCancelled_ToFabricAsync(PlaneSendRequestCancelledDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_FabricStreSend_PlaneSubscribe_ToFabricAsyncamingRequest_ToServerAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_PlaneSubscribe_ToFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

            PlaneFabricConverter.WriteClientToHostMessageType(writer, PlaneFabricClientToHostMessageEnum.PlaneSendRequestCancelled);
            writer.Write(message);
        }, ct);
    }
    public async Task Send_PlaneSendRequestDone_ToFabricAsync(PlaneSendRequestDoneDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_PlaneSendRequestDone_ToFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_PlaneSendRequestDone_ToFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

            PlaneFabricConverter.WriteClientToHostMessageType(writer, PlaneFabricClientToHostMessageEnum.PlaneSendRequestDone);
            writer.Write(message);
        }, ct);
    }

    public async Task Send_PlaneInvokeRequest_ToFabricAsync(PlaneInvokeRequestDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_PlaneInvokeRequest_ToFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        await EnqueueAndWaitAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_PlaneInvokeRequest_ToFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

            PlaneFabricConverter.WriteClientToHostMessageType(writer, PlaneFabricClientToHostMessageEnum.PlaneInvokeRequest);
            writer.Write(message);
        }, ct);
    }
    public async Task Send_PlaneInvokeRequestCancelled_ToFabricAsync(PlaneInvokeRequestCancelledDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_PlaneInvokeRequestCancelled_ToFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_PlaneInvokeRequestCancelled_ToFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

            PlaneFabricConverter.WriteClientToHostMessageType(writer, PlaneFabricClientToHostMessageEnum.PlaneInvokeRequestCancelled);
            writer.Write(message);
        }, ct);
    }
    public async Task Send_PlaneInvokeRequestAck_ToFabricAsync(PlaneInvokeRequestAckDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_PlaneInvokeRequestAck_ToFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_PlaneInvokeRequestAck_ToFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

            PlaneFabricConverter.WriteClientToHostMessageType(writer, PlaneFabricClientToHostMessageEnum.PlaneInvokeRequestAck);
            writer.Write(message);
        }, ct);
    }
    public async Task Send_PlaneInvokeRequestDone_ToFabricAsync(PlaneInvokeRequestDoneDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_PlaneInvokeRequestDone_ToFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_PlaneInvokeRequestDone_ToFabricAsync({RequestId}, {ServiceId}, {MethodId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId);

            PlaneFabricConverter.WriteClientToHostMessageType(writer, PlaneFabricClientToHostMessageEnum.PlaneInvokeRequestDone);
            writer.Write(message);
        }, ct);
    }

    public async Task Send_PlaneStreamingRequest_ToFabricAsync(PlaneStreamingRequestDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_PlaneStreamingRequest_ToFabricAsync({RequestId}, {ServiceId}, {MethodId}, {index})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId, message.ArgumentIndex);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_PlaneStreamingRequest_ToFabricAsync({RequestId}, {ServiceId}, {MethodId}, {index})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId, message.ArgumentIndex);

            PlaneFabricConverter.WriteClientToHostMessageType(writer, PlaneFabricClientToHostMessageEnum.PlaneStreamingRequest);
            writer.Write(message);
        }, ct);
    }
    public async Task Send_PlaneStreamingResponse_ToFabricAsync(PlaneStreamingResponseDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_PlaneStreamingResponse_ToFabricAsync({RequestId}, {ServiceId}, {MethodId}, {index})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId, message.ArgumentIndex);

        await EnqueueAndWaitAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_PlaneStreamingResponse_ToFabricAsync({RequestId}, {ServiceId}, {MethodId}, {index})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.RequestId, message.Routing.ServiceId, message.Routing.MethodId, message.ArgumentIndex);

            PlaneFabricConverter.WriteClientToHostMessageType(writer, PlaneFabricClientToHostMessageEnum.PlaneStreamingResponse);
            writer.Write(message);
        }, ct);
    }

    record SendQueueItem(
        Action<BinaryWriter> Write,
        TaskCompletionSource? Sent);
}