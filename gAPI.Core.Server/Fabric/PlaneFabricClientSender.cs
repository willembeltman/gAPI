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
            Logger.LogError(ex, "RegisterAsyncEnumerableArgument");
        }
    }
    private async Task EnqueueAndWaitAsync(Action<BinaryWriter> write, CancellationToken ct)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        try
        {
            await SendQueue.Writer.WriteAsync(new SendQueueItem(write, completion), ct);
        }
        catch (TaskCanceledException ex)
        {
            Logger.LogError(ex, "RegisterAsyncEnumerableArgument");
        }

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