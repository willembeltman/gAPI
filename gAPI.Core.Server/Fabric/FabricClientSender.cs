using gAPI.Core.Dtos;
using gAPI.Core.Ids;
using gAPI.Core.Interfaces;
using gAPI.Core.Server.Enums;
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
    public async Task Send_ClearSession_ToFabricAsync(ClearSessionDto clearSessionDto, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Send_ClearSession_ToFabricAsync({clearSessionDto})", clearSessionDto);
        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.ClearSession);
            writer.Write(clearSessionDto);
        }, ct);
    }
    public async Task Send_GetSession_ToFabricAsync(GetSessionCookieDataDto getSessionDto, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Send_GetSession_ToFabricAsync({getSessionDto})", getSessionDto);
        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.GetSessionCookieData);
            writer.Write(getSessionDto);
        }, ct);
    }

    public async Task Send_Subscribe_ToFabricAsync(ISseHost sseHost, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Send_Subscribe_ToFabricAsync({sseHost})", sseHost);
        var request = new SubscribeDto()
        {
            ServiceId = sseHost.ServiceId,
            UserId = sseHost.UserId,
            SessionId = sseHost.SessionId
        };
        await EnqueueAsync(w =>
        {
            FabricConverter.WriteClientToHostMessageType(w, FabricClientToHostMessageEnum.Subscribe);
            w.Write(request);
        }, ct);
    }
    public async Task Send_Unsubscribe_ToFabricAsync(ISseHost sseHost, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Send_Unsubscribe_ToFabricAsync({sseHost})", sseHost);
        var request = new UnsubscribeDto()
        {
            ServiceId = sseHost.ServiceId,
            UserId = sseHost.UserId,
            SessionId = sseHost.SessionId
        };
        await EnqueueAsync(w =>
        {
            FabricConverter.WriteClientToHostMessageType(w, FabricClientToHostMessageEnum.Unsubscribe);
            w.Write(request);
        }, ct);
    }

    public async Task Send_SendRequest_ToFabricAsync(SendRequestDto request, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Send_SendRequest_ToFabricAsync({request})", request);
        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.SendRequest);
            writer.Write(request);
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
    public async Task Send_InvokeResponseDone_ToFabricAsync(RequestId requestId, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Send_InvokeResponseDone_ToFabricAsync({requestId})", requestId);

        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.InvokeResponseDone);
            writer.Write(requestId);
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