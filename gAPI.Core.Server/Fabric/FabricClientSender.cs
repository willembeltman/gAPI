using gAPI.Core.Dtos;
using gAPI.Core.Ids;
using gAPI.Core.Server.Enums;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace gAPI.Core.Server.Fabric;

public class FabricClientSender(
    ILoggerFactory loggerFactory)
{
    private readonly ILogger Logger = loggerFactory.CreateLogger<FabricClientSender>();
    readonly Channel<Action<BinaryWriter>> SendQueue = Channel.CreateUnbounded<Action<BinaryWriter>>();

    public async Task SendKernel(BinaryWriter binaryWriter, CancellationToken ct)
    {
        await foreach (var item in SendQueue.Reader.ReadAllAsync(ct))
        {
            while (binaryWriter == null)
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
            Logger.LogTrace("{now} Send_UpdateSession_ToFabricAsync({updateSessionDto})", DateTime.Now.ToString("HH:mm:ss.fff"), updateSessionDto);

        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.UpdateSession);
            writer.Write(updateSessionDto);
        }, ct);
    }
    public async Task Send_ClearSession_ToFabricAsync(SendClearSessionDto clearSessionDto, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Send_ClearSession_ToFabricAsync({clearSessionDto})", DateTime.Now.ToString("HH:mm:ss.fff"), clearSessionDto);

        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.ClearSession);
            writer.Write(clearSessionDto);
        }, ct);
    }
    public async Task Send_GetSession_ToFabricAsync(SendGetSessionCookieDataDto getSessionDto, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Send_GetSession_ToFabricAsync({getSessionDto})", DateTime.Now.ToString("HH:mm:ss.fff"), getSessionDto);

        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.GetSessionCookieData);
            writer.Write(getSessionDto);
        }, ct);
    }

    public async Task Send_Subscribe_ToFabricAsync(SubscribeDto subscribe, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Send_Subscribe_ToFabricAsync({subsciption})", DateTime.Now.ToString("HH:mm:ss.fff"), subscribe);

        await EnqueueAsync(w =>
        {
            FabricConverter.WriteClientToHostMessageType(w, FabricClientToHostMessageEnum.Subscribe);
            w.Write(subscribe);
        }, ct);
    }
    public async Task Send_Unsubscribe_ToFabricAsync(UnsubscribeDto unsubscribe, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Send_Unsubscribe_ToFabricAsync({unsubscribe})", DateTime.Now.ToString("HH:mm:ss.fff"), unsubscribe);

        await EnqueueAsync(w =>
        {
            FabricConverter.WriteClientToHostMessageType(w, FabricClientToHostMessageEnum.Unsubscribe);
            w.Write(unsubscribe);
        }, ct);
    }

    public async Task Send_SendRequest_ToFabricAsync(SendRequestDto request, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Send_SendRequest_ToFabricAsync({request})", DateTime.Now.ToString("HH:mm:ss.fff"), request);

        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.SendRequest);
            writer.Write(request);
        }, ct);
    }
    public async Task Send_SendRequestCancelled_ToFabricAsync(SendRequestCancelledDto cancel, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Send_SendRequestCancelled_ToFabricAsync({cancel})", DateTime.Now.ToString("HH:mm:ss.fff"), cancel);

        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.SendRequestCancelled);
            writer.Write(cancel);
        }, ct);
    }
    public async Task Send_SendRequestDone_ToFabricAsync(SendRequestDoneDto done, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Send_SendRequestDone_ToFabricAsync({done})", DateTime.Now.ToString("HH:mm:ss.fff"), done);

        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.SendRequestDone);
            writer.Write(done);
        }, ct);
    }

    public async Task Send_InvokeRequest_ToFabricAsync(InvokeRequestDto request, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Send_InvokeRequest_ToFabricAsync({request})", DateTime.Now.ToString("HH:mm:ss.fff"), request);

        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.InvokeRequest);
            writer.Write(request);
        }, ct);
    }
    public async Task Send_InvokeRequestCancelled_ToFabricAsync(InvokeRequestCancelledDto cancel, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Send_InvokeRequestCancelled_ToFabricAsync({cancel})", DateTime.Now.ToString("HH:mm:ss.fff"), cancel);

        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.InvokeRequestCancelled);
            writer.Write(cancel);
        }, ct);
    }
    public async Task Send_InvokeRequestDone_ToFabricAsync(InvokeRequestDoneDto response, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Send_InvokeRequestDone_ToFabricAsync({requestId})", DateTime.Now.ToString("HH:mm:ss.fff"), response);

        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.InvokeRequestDone);
            writer.Write(response);
        }, ct);
    }

    public async Task Send_StreamingRequestServerToClient_ToFabricAsync(StreamingRequestDto request, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Send_StreamingRequestServerToClient_ToFabricAsync({request})", DateTime.Now.ToString("HH:mm:ss.fff"), request);

        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.StreamingRequestServerToClient);
            writer.Write(request);
        }, ct);
    }
    public async Task Send_StreamingResponseServerToClient_ToFabricAsync(StreamingResponseDto response, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Send_StreamingResponseServerToClient_ToFabricAsync({response})", DateTime.Now.ToString("HH:mm:ss.fff"), response);

        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.StreamingResponseServerToClient);
            writer.Write(response);
        }, ct);
    }
    public async Task Send_StreamingRequestClientToServer_ToFabricAsync(StreamingRequestDto request, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Send_StreamingRequestClientToServer_ToFabricAsync({request})", DateTime.Now.ToString("HH:mm:ss.fff"), request);

        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.StreamingRequestClientToServer);
            writer.Write(request);
        }, ct);
    }
    public async Task Send_StreamingResponseClientToServer_ToFabricAsync(StreamingResponseDto response, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now} Send_StreamingResponseClientToServer_ToFabricAsync({response})", DateTime.Now.ToString("HH:mm:ss.fff"), response);

        await EnqueueAsync(writer =>
        {
            FabricConverter.WriteClientToHostMessageType(writer, FabricClientToHostMessageEnum.StreamingResponseClientToServer);
            writer.Write(response);
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