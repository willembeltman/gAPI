using gAPI.Core.Dtos;
using gAPI.Core.Ids;
using gAPI.Core.Server.Enums;
using gAPI.Core.Server.Interfaces;
using Microsoft.Extensions.Logging;

namespace gAPI.Core.Server.Fabric;

public class FabricClientSender(
    FabricClient fabricClient,
    ILoggerFactory loggerFactory)
{
    private readonly ILogger Logger = loggerFactory.CreateLogger<FabricClientSender>();

    public async Task SendKernel(CancellationToken ct)
    {
        await foreach (var item in fabricClient.SendQueue.Reader.ReadAllAsync(ct))
        {
            while (fabricClient.BinaryWriter == null)
            {
                await Task.Delay(10, ct);
            }
            item(fabricClient.BinaryWriter);
            fabricClient.BinaryWriter.Flush();
        }
    }

    public async Task Send_UpdateSession_ToFabricAsync(UpdateSessionDto updateSessionDto, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Send_UpdateSession_ToFabricAsync({updateSessionDto})", updateSessionDto);
        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.UpdateSession);
            writer.Write(updateSessionDto);
        }, ct);
    }
    public async Task Send_ClearSession_ToFabricAsync(SendClearSessionDto clearSessionDto, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Send_ClearSession_ToFabricAsync({clearSessionDto})", clearSessionDto);
        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.ClearSession);
            writer.Write(clearSessionDto);
        }, ct);
    }
    public async Task Send_GetSession_ToFabricAsync(SendGetSessionCookieDataDto getSessionDto, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Send_GetSession_ToFabricAsync({getSessionDto})", getSessionDto);
        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.GetSessionCookieData);
            writer.Write(getSessionDto);
        }, ct);
    }

    public async Task Send_Subscribe_ToFabricAsync(IServiceSubscription SseServiceSubscription, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Send_Subscribe_ToFabricAsync({SseServiceSubscription})", SseServiceSubscription);
        var request = new SubscribeDto()
        {
            ServiceId = SseServiceSubscription.ServiceId,
            UserId = SseServiceSubscription.UserId,
            SessionId = SseServiceSubscription.SessionId
        };
        await EnqueueAsync(w =>
        {
            FabricConverter.WriteClientToHostMessageType(w, FabricClientToHostMessageEnum.Subscribe);
            w.Write(request);
        }, ct);
    }
    public async Task Send_Unsubscribe_ToFabricAsync(IServiceSubscription SseServiceSubscription, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Send_Unsubscribe_ToFabricAsync({SseServiceSubscription})", SseServiceSubscription);
        var request = new UnsubscribeDto()
        {
            ServiceId = SseServiceSubscription.ServiceId,
            UserId = SseServiceSubscription.UserId,
            SessionId = SseServiceSubscription.SessionId
        };
        await EnqueueAsync(w =>
        {
            FabricConverter.WriteClientToHostMessageType(w, FabricClientToHostMessageEnum.Unsubscribe);
            w.Write(request);
        }, ct);
    }

    public async Task Send_SendRequest_ToFabricAsync2(SendRequestDto request, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Send_SendRequest_ToFabricAsync({request})", request);
        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.SendRequest);
            writer.Write(request);
        }, ct);
    }
    public async Task Send_SendRequestDone_ToFabricAsync(SendRequestDoneDto done, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Send_SendRequestDone_ToFabricAsync({request})", done);
        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.SendRequestDone);
            writer.Write(done);
        }, ct);
    }
    public async Task Send_SendRequestException_ToFabricAsync(SendRequestExceptionDto ex, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Send_SendRequestDone_ToFabricAsync({ex})", ex);
        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.SendRequestException);
            writer.Write(ex);
        }, ct);
    }
    public async Task Send_InvokeRequest_ToFabricAsync(InvokeRequestDto request, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Send_InvokeRequest_ToFabricAsync({request})", request);
        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.InvokeRequest);
            writer.Write(request);
        }, ct);
    }
    public async Task Send_InvokeArgumentRequest_ToFabricAsync(InvokeArgumentRequestDto request, CancellationToken ct)
    {
        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.InvokeArgumentRequest);
            writer.Write(request);
        }, ct);
    }
    public async Task Send_InvokeArgumentResponse_ToFabricAsync(InvokeArgumentResponseDto response, CancellationToken ct)
    {
        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.InvokeArgumentResponse);
            writer.Write(response);
        }, ct);
    }
    public async Task Send_InvokeResponse_ToFabricAsync(InvokeResponseDto response, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Send_InvokeResponse_ToFabricAsync({response})", response);

        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.InvokeResponse);
            writer.Write(response);
        }, ct);
    }
    public async Task Send_InvokeResponseDone_ToFabricAsync(InvokeResponseDoneDto response, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Send_InvokeResponseDone_ToFabricAsync({requestId})", response);

        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.InvokeResponseDone);
            writer.Write(response);
        }, ct);
    }
    public async Task Send_InvokeResponseException_ToFabricAsync(InvokeResponseExceptionDto ex, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Send_InvokeResponseDone_ToFabricAsync({ex})", ex);

        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.InvokeResponseException);
            writer.Write(ex);
        }, ct);
    }
    private async Task EnqueueAsync(Action<BinaryWriter> write, CancellationToken ct)
    {
        try
        {
            await fabricClient.SendQueue.Writer.WriteAsync(write, ct);
        }
        catch (TaskCanceledException)
        {
        }
    }

}