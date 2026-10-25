using gAPI.Core.Dtos;
using gAPI.Core.Ids;
using gAPI.Core.Server.Enums;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace gAPI.Core.Server.Fabric;

public class FabricClientSender(
FabricClient fabricClient, ILoggerFactory loggerFactory)
{
    private readonly ILogger Logger = loggerFactory.CreateLogger<FabricClientSender>();
    readonly Channel<Action<BinaryWriter>> SendQueue = Channel.CreateUnbounded<Action<BinaryWriter>>();

    public async Task SendKernel(CancellationToken ct)
    {
        await foreach (var item in SendQueue.Reader.ReadAllAsync(ct))
        {
            while (fabricClient.IsConnected == false || fabricClient.BinaryWriter == null)
            {
                await Task.Delay(10, ct);
            }
            try
            {
                var writer = fabricClient.BinaryWriter;
                if (writer != null)
                {
                    item(writer);
                    writer.Flush();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error writing to Fabric stream");
            }
        }
    }

    public async Task Send_UpdateSession_ToFabricAsync(UpdateSessionDto updateSessionDto, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_UpdateSession_ToFabricAsync({sessionId})", DateTime.Now.ToString("HH:mm:ss.fff"), updateSessionDto.SessionId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_UpdateSession_ToFabricAsync({sessionId})", DateTime.Now.ToString("HH:mm:ss.fff"), updateSessionDto.SessionId);

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
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_GetSession_ToFabricAsync({getSessionDto})", DateTime.Now.ToString("HH:mm:ss.fff"), getSessionDto);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_GetSession_ToFabricAsync({getSessionDto})", DateTime.Now.ToString("HH:mm:ss.fff"), getSessionDto);

            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.GetSessionCookieData);
            writer.Write(getSessionDto);
        }, ct);
    }

    public async Task Send_Subscribe_ToFabricAsync(SubscribeDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_Subscribe_ToFabricAsync({serviceId}, {sessionId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.ServiceId, message.SessionId);

        await EnqueueAsync(w =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_Subscribe_ToFabricAsync({serviceId}, {sessionId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.ServiceId, message.SessionId);

            FabricConverter.WriteClientToHostMessageType(w, FabricClientToHostMessageEnum.Subscribe);
            w.Write(message);
        }, ct);
    }
    public async Task Send_Unsubscribe_ToFabricAsync(UnsubscribeDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_Unsubscribe_ToFabricAsync({serviceId}, {sessionId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.ServiceId, message.SessionId);

        await EnqueueAsync(w =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_Unsubscribe_ToFabricAsync({serviceId}, {sessionId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.ServiceId, message.SessionId);

            FabricConverter.WriteClientToHostMessageType(w, FabricClientToHostMessageEnum.Unsubscribe);
            w.Write(message);
        }, ct);
    }

    public async Task Send_SendAndForgetRequest_ToFabricAsync(SendRequestDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_SendAndForgetRequest_ToFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_SendAndForgetRequest_ToFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.SendAndForgetRequest);
            writer.Write(message);
        }, ct);
    }
    public async Task Send_SendAndForgetRequestCancelled_ToFabricAsync(SendRequestCancelledDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_SendAndForgetRequestCancelled_ToFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_SendAndForgetRequestCancelled_ToFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.SendAndForgetRequestCancelled);
            writer.Write(message);
        }, ct);
    }

    public async Task Send_SendRequest_ToFabricAsync(SendRequestDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_SendRequest_ToFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_SendRequest_ToFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.SendRequest);
            writer.Write(message);
        }, ct);
    }
    public async Task Send_SendRequestCancelled_ToFabricAsync(SendRequestCancelledDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_SendRequestCancelled_ToFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_SendRequestCancelled_ToFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.SendRequestCancelled);
            writer.Write(message);
        }, ct);
    }
    public async Task Send_SendRequestDone_ToFabricAsync(SendRequestDoneDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_SendRequestDone_ToFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_SendRequestDone_ToFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.SendRequestDone);
            writer.Write(message);
        }, ct);
    }

    public async Task Send_InvokeRequest_ToFabricAsync(InvokeRequestDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_InvokeRequest_ToFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_InvokeRequest_ToFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.InvokeRequest);
            writer.Write(message);
        }, ct);
    }
    public async Task Send_InvokeRequestCancelled_ToFabricAsync(InvokeRequestCancelledDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_InvokeRequestCancelled_ToFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_InvokeRequestCancelled_ToFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.InvokeRequestCancelled);
            writer.Write(message);
        }, ct);
    }
    public async Task Send_InvokeRequestAck_ToFabricAsync(InvokeRequestAckDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Send_InvokeRequestAck_ToFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_InvokeRequestAck_ToFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.InvokeRequestAck);
            writer.Write(message);
        }, ct);
    }
    public async Task Send_InvokeRequestDone_ToFabricAsync(InvokeRequestDoneDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Send_InvokeRequestDone_ToFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_InvokeRequestDone_ToFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.InvokeRequestDone);
            writer.Write(message);
        }, ct);
    }

    public async Task Send_StreamingResponse_ToFabricAsync(StreamingResponseDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_StreamingResponse_ResponseEnumerable_ToFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_StreamingResponse_ResponseEnumerable_ToFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.StreamingResponse);
            writer.Write(message);
        }, ct);
    }
    public async Task Send_StreamingRequest_ToFabricAsync(StreamingRequestDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_StreamingRequest_ArgumentEnumerable_ToFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_StreamingRequest_ArgumentEnumerable_ToFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.StreamingRequest);
            writer.Write(message);
        }, ct);
    }


    private async Task EnqueueAsync(Action<BinaryWriter> write, CancellationToken ct)
    {
        try
        {
            await SendQueue.Writer.WriteAsync(write, ct);
        }
        catch (TaskCanceledException)
        {
        }
    }

    //internal async Task Send_BusSubscribe_ToFabricAsync(BusSubscribeDto subscribe, CancellationToken ct)
    //{
    //    await EnqueueAsync(writer =>
    //    {
    //        FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.BusSubscribe);
    //        writer.Write(subscribe);
    //    }, ct);
    //}
    //internal async Task Send_BusUnsubscribe_ToFabricAsync(BusUnsubscribeDto unsubscribe, CancellationToken ct)
    //{
    //    await EnqueueAsync(writer =>
    //    {
    //        FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.BusUnsubscribe);
    //        writer.Write(unsubscribe);
    //    }, ct);
    //}

    //internal async Task Send_BusSendAndForgetRequest_ToFabricAsync(BusSendRequestDto request, CancellationToken ct)
    //{
    //    await EnqueueAsync(writer =>
    //    {
    //        FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.BusFireAndForget);
    //        writer.Write(request);
    //    }, ct);
    //}
    //internal async Task Send_BusSendAndForgetRequestCancelled_ToFabricAsync(BusSendRequestCancelledDto request, CancellationToken ct)
    //{
    //    await EnqueueAsync(writer =>
    //    {
    //        FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.BusFireAndForgetCancelled);
    //        writer.Write(request);
    //    }, ct);
    //}

    //internal async Task Send_BusSendRequest_ToFabricAsync(BusSendRequestDto request, CancellationToken ct)
    //{
    //    await EnqueueAsync(writer =>
    //    {
    //        FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.BusSendRequest);
    //        writer.Write(request);
    //    }, ct);
    //}
    //internal async Task Send_BusSendCancelled_ToFabricAsync(BusSendRequestCancelledDto request, CancellationToken ct)
    //{
    //    await EnqueueAsync(writer =>
    //    {
    //        FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.BusSendRequestCancelled);
    //        writer.Write(request);
    //    }, ct);
    //}
    //internal async Task Send_BusSendRequestDone_ToFabricAsync(BusSendRequestDoneDto ack, CancellationToken ct)
    //{
    //    await EnqueueAsync(writer =>
    //    {
    //        FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.BusSendRequestDone);
    //        writer.Write(ack);
    //    }, ct);
    //}

    //internal async Task Send_BusInvokeRequest_ToFabricAsync(BusInvokeRequestDto request, CancellationToken ct)
    //{
    //    await EnqueueAsync(writer =>
    //    {
    //        FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.BusInvokeRequest);
    //        writer.Write(request);
    //    }, ct);
    //}
    //internal async Task Send_BusInvokeRequestCancelled_ToFabricAsync(BusInvokeRequestCancelledDto request, CancellationToken ct)
    //{
    //    await EnqueueAsync(writer =>
    //    {
    //        FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.BusInvokeRequestCancelled);
    //        writer.Write(request);
    //    }, ct);
    //}
    //internal async Task Send_BusInvokeRequestAck_ToFabricAsync(BusInvokeRequestAckDto ack, CancellationToken ct)
    //{
    //    await EnqueueAsync(writer =>
    //    {
    //        FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.BusInvokeRequestAck);
    //        writer.Write(ack);
    //    }, ct);
    //}
    //internal async Task Send_BusInvokeRequestDone_ToFabricAsync(BusInvokeRequestDoneDto done, CancellationToken ct)
    //{
    //    await EnqueueAsync(writer =>
    //    {
    //        FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.BusInvokeRequestDone);
    //        writer.Write(done);
    //    }, ct);
    //}

    //internal async Task Send_BusStreamingRequest_ToFabricAsync(BusStreamingRequestDto busStreamingRequestDto, CancellationToken ct)
    //{
    //    await EnqueueAsync(writer =>
    //    {
    //        FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.BusStreamingRequest);
    //        writer.Write(busStreamingRequestDto);
    //    }, ct);
    //}
    //internal async Task Send_BusStreamingResponse_ToFabricAsync(BusStreamingResponseDto response, CancellationToken ct)
    //{
    //    await EnqueueAsync(writer =>
    //    {
    //        FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.BusStreamingResponse);
    //        writer.Write(response);
    //    }, ct);
    //}
}