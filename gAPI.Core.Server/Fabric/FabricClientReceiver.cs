using gAPI.Core.Dtos;
using gAPI.Core.Ids;
using gAPI.Core.Interfaces;
using gAPI.Core.Server.Enums;
using gAPI.Core.Wss;
using Microsoft.Extensions.Logging;

namespace gAPI.Core.Server.Fabric;

public class FabricClientReceiver(
    FabricClient fabricClient,
    ILoggerFactory loggerFactory)
{
    readonly ILogger Logger = loggerFactory.CreateLogger<FabricClientReceiver>();

    public FabricHostId? Id { get => fabricClient.Id; private set => fabricClient.Id = value; }

    public async Task ReceiveKernel(CancellationToken ct)
    {
        if (fabricClient.BinaryReader == null) return;
        try
        {
            Id = FabricConverter.ReadFabricHostId(fabricClient.BinaryReader);

            if (Logger.IsEnabled(LogLevel.Warning))
                Logger.LogTrace(
                    "FabricClient {Id.Value} started",
                    Id.Value);

            while (!ct.IsCancellationRequested)
            {
                var messageType = FabricConverter.ReadHostToClientMessageType(fabricClient.BinaryReader);
                switch (messageType)
                {
                    case FabricHostToClientMessageEnum.SendRequest:
                        var sendRequest = fabricClient.BinaryReader.ReadSendRequestDto();
                        _ = Task.Run(async () => { await Receive_SendRequest_FromFabricAsync(sendRequest, ct); }, ct);
                        break;
                    case FabricHostToClientMessageEnum.SendRequestDone:
                        var sendArgumentedRequestDone = fabricClient.BinaryReader.ReadSendRequestDoneDto();
                        await Receive_SendRequestDone_FromFabricAsync(sendArgumentedRequestDone, ct);
                        break;
                    case FabricHostToClientMessageEnum.InvokeArgumentRequest:
                        var argumentRequest = fabricClient.BinaryReader.ReadInvokeArgumentRequestDto();
                        await Receive_InvokeArgumentRequest_FromFabricAsync(argumentRequest, ct);
                        break;
                    case FabricHostToClientMessageEnum.InvokeArgumentResponse:
                        var argumentResponse = fabricClient.BinaryReader.ReadInvokeArgumentResponseDto();
                        await Receive_InvokeArgumentResponse_FromFabricAsync(argumentResponse, ct);
                        break;
                    case FabricHostToClientMessageEnum.InvokeRequest:
                        var invokeRequest = fabricClient.BinaryReader.ReadInvokeRequestDto();
                        _ = Task.Run(async () => { await Receive_InvokeRequest_FromFabricAsync(invokeRequest, ct); }, ct);
                        break;
                    case FabricHostToClientMessageEnum.InvokeResponse:
                        var invokeResponse = fabricClient.BinaryReader.ReadInvokeResponseDto();
                        await Receive_InvokeResponse_FromFabricAsync(invokeResponse, ct);
                        break;
                    case FabricHostToClientMessageEnum.InvokeResponseDone:
                        var invokeResponseDone = fabricClient.BinaryReader.ReadInvokeResponseDoneDto();
                        await Receive_InvokeResponseDone_FromFabricAsync(invokeResponseDone, ct);
                        break;
                    case FabricHostToClientMessageEnum.GetSessionCookieDataResponse:
                        var activate = fabricClient.BinaryReader.ReadSendGetSessionCookieDataResponseDto();
                        await Receive_GetSessionResponse_FromFabricAsync(activate, ct);
                        break;
                        //case FabricHostToClientMessageEnum.Log:
                        //    var log = fabricClient.BinaryReader.ReadWssLoggerLogDto();
                        //    //_ = Task.Run(async () => { await Receive_Log_FromFabricAsync(log, ct); }, ct);
                        //    await Receive_Log_FromFabricAsync(log, ct);
                        //    break;
                }
            }
        }
        catch (Exception ex)
        {
            if (Logger.IsEnabled(LogLevel.Warning))
            {
                Logger.LogWarning(
                    "FabricClient #{Id.Value}: Exception occured, restarting fabric client\r\n{ex}",
                    Id?.Value,
                    ex);
            }
        }

        await fabricClient.ReconnectAsync(ct); // Letop deze moet naar boven
    }

    private async Task Receive_SendRequest_FromFabricAsync(SendRequestDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Receive_SendRequest_FromFabricAsync({message})", message);

        try
        {
            await fabricClient.Send_SendRequest_ToClient_Async(message, ct);
            await fabricClient.Sender.Send_SendRequestDone_ToFabricAsync(
                new SendRequestDoneDto(
                    message.RequestId,
                    message.ServiceId,
                    message.MethodId,
                    message.UserId, 
                    message.SessionId,
                    message.StateIsChanged,
                    message.StateData,
                    null
                ), ct);
        }
        catch (Exception ex)
        {
            await fabricClient.Sender.Send_SendRequestDone_ToFabricAsync(
                new SendRequestDoneDto(
                    message.RequestId,
                    message.ServiceId,
                    message.MethodId,
                    message.UserId,
                    message.SessionId,
                    message.StateIsChanged,
                    message.StateData, 
                    ex.Message
                ), ct);
        }
    }
    private async Task Receive_SendRequestDone_FromFabricAsync(SendRequestDoneDto done, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Receive_SendRequestDone_FromFabricAsync({done})", done);

        await fabricClient.Receive_SendRequestDone_FromFabricAsync(done);
    }
    private async Task Receive_InvokeArgumentRequest_FromFabricAsync(InvokeArgumentRequestDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Receive_InvokeArgumentRequest_FromFabricAsync({message})", message);

        await fabricClient.Receive_InvokeArgumentRequest_FromFabricAsync(message, ct);
    }
    private async Task Receive_InvokeArgumentResponse_FromFabricAsync(InvokeArgumentResponseDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Receive_InvokeArgumentResponse_FromFabricAsync({message})", message);

        await fabricClient.Receive_InvokeArgumentResponse_FromFabricAsync(message, ct);

    }
    private async Task Receive_InvokeRequest_FromFabricAsync(InvokeRequestDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(
                "Receive_InvokeRequest_FromFabricAsync({message})",
                message);

        try
        {
            var list = fabricClient.Send_InvokeRequest_ToClientAsync(message, ct);

            await foreach (var item in list)
                await fabricClient.Sender.Send_InvokeResponse_ToFabricAsync(item, ct);

            // Send done for this host
            await fabricClient.Sender.Send_InvokeResponseDone_ToFabricAsync(
                new InvokeResponseDoneDto(
                    message.RequestId,
                    message.ServiceId,
                    message.MethodId,
                    message.UserId,
                    message.SessionId,
                    null
                ), ct);
        }
        catch (Exception ex)
        {
            await fabricClient.Sender.Send_InvokeResponseDone_ToFabricAsync(
                new InvokeResponseDoneDto(
                    message.RequestId,
                    message.ServiceId,
                    message.MethodId,
                    message.UserId,
                    message.SessionId,
                    ex.Message
                ), ct);
        }
    }
    private async Task Receive_InvokeResponse_FromFabricAsync(InvokeResponseDto response, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Receive_InvokeResponse_FromFabricAsync({response})", response);

        await fabricClient.Receive_InvokeResponse_FromFabricAsync(response);
    }
    private async Task Receive_InvokeResponseDone_FromFabricAsync(InvokeResponseDoneDto invokeResponseDone, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Receive_InvokeResponseDone_FromFabricAsync({invokeResponseDone})", invokeResponseDone);

        await fabricClient.Receive_InvokeResponseDone_FromFabricAsync(invokeResponseDone);
    }

    private async Task Receive_GetSessionResponse_FromFabricAsync(SendGetSessionCookieDataResponseDto getSessionResponse, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Receive_GetSessionResponse_FromFabricAsync({getSessionResponse})", getSessionResponse);

        await fabricClient.Receive_GetSessionResponse_FromFabricAsync(getSessionResponse);
    }

    //private async Task Receive_Log_FromFabricAsync(WssLoggerLogDto log, CancellationToken ct)
    //{
    //    if (log.Category == null)
    //        return;
    //    var logger = loggerFactory.CreateLogger(log.Category);
    //    logger.Log(
    //        log.Level,
    //        log.Message,
    //        log.Data?
    //            .Select(a => new KeyValuePair<string, string?>(a.Key, a.Value))
    //            .ToArray()
    //    );
    //}
}
