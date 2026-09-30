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

    public async Task SendKernel(BinaryWriter binaryWriter, CancellationToken ct)
    {
        await foreach (var item in SendQueue.Reader.ReadAllAsync(ct))
        {
            while (fabricClient.IsConnected == false)
            {
                await Task.Delay(10, ct);
            }
            item(binaryWriter);
            binaryWriter.Flush();
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

    public async Task Send_StreamingRequestServerToClient_ToFabricAsync(StreamingRequestDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_StreamingRequestServerToClient_ToFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_StreamingRequestServerToClient_ToFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.StreamingRequestServerToClient);
            writer.Write(message);
        }, ct);
    }
    public async Task Send_StreamingResponseServerToClient_ToFabricAsync(StreamingResponseDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_StreamingResponseServerToClient_ToFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_StreamingResponseServerToClient_ToFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.StreamingResponseServerToClient);
            writer.Write(message);
        }, ct);
    }

    public async Task Send_StreamingRequestClientToServer_ToFabricAsync(StreamingRequestDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_StreamingRequestClientToServer_ToFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_StreamingRequestClientToServer_ToFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.StreamingRequestClientToServer);
            writer.Write(message);
        }, ct);
    }
    public async Task Send_StreamingResponseClientToServer_ToFabricAsync(StreamingResponseDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_StreamingResponseClientToServer_ToFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_StreamingResponseClientToServer_ToFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.StreamingResponseClientToServer);
            writer.Write(message);
        }, ct);
    }



    public async Task Send_ServerSendRequest_ToFabricAsync(ServerSendRequestDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_ServerSendRequest_ToFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_ServerSendRequest_ToFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.ServerSendRequest);
            writer.Write(message);
        }, ct);
    }
    public async Task Send_ServerSendRequestCancelled_ToFabricAsync(ServerSendRequestCancelledDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_ServerSendRequestCancelled_ToFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_ServerSendRequestCancelled_ToFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.ServerSendRequestCancelled);
            writer.Write(message);
        }, ct);
    }
    public async Task Send_ServerSendRequestDone_ToFabricAsync(ServerSendRequestDoneDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_ServerSendRequestDone_ToFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_ServerSendRequestDone_ToFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.ServerSendRequestDone);
            writer.Write(message);
        }, ct);
    }
    public async Task Send_ServerInvokeRequest_ToFabricAsync(ServerInvokeRequestDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_ServerInvokeRequest_ToFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_ServerInvokeRequest_ToFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.ServerInvokeRequest);
            writer.Write(message);
        }, ct);
    }
    public async Task Send_ServerInvokeRequestCancelled_ToFabricAsync(ServerInvokeRequestCancelledDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_ServerInvokeRequestCancelled_ToFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_ServerInvokeRequestCancelled_ToFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.ServerInvokeRequestCancelled);
            writer.Write(message);
        }, ct);
    }
    public async Task Send_ServerInvokeRequestDone_ToFabricAsync(ServerInvokeRequestDoneDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Send_ServerInvokeRequestDone_ToFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_ServerInvokeRequestDone_ToFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.ServerInvokeRequestDone);
            writer.Write(message);
        }, ct);
    }
    public async Task Send_ServerStreamingRequest_ToFabricAsync(ServerStreamingRequestDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_ServerStreamingRequest_ToFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_ServerStreamingRequest_ToFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.ServerStreamingRequest);
            writer.Write(message);
        }, ct);
    }
    public async Task Send_ServerStreamingResponse_ToFabricAsync(ServerStreamingResponseDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} QUEUED Send_ServerStreamingResponse_ToFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

        await EnqueueAsync(writer =>
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace("{now} Send_ServerStreamingResponse_ToFabricAsync({serviceId}, {methodId}, {requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), message.Routing.ServiceId, message.Routing.MethodId, message.Routing.RequestId);

            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.ServerStreamingResponse);
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

}