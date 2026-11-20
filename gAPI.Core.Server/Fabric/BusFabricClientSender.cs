using gAPI.Core.Dtos;
using gAPI.Core.Ids;
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