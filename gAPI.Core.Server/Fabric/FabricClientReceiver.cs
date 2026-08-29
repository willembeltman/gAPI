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
                        await Receive_SendRequest_FromFabricAsync(sendRequest, ct);
                        break;
                    case FabricHostToClientMessageEnum.SendArgumentedRequest:
                        var sendArgumentedRequest = fabricClient.BinaryReader.ReadSendRequestDto();
                        _ = Task.Run(async () => { await Receive_SendArgumentedRequest_FromFabricAsync(sendArgumentedRequest, ct); }, ct);
                        break;
                    case FabricHostToClientMessageEnum.SendArgumentedRequestDone:
                        var sendArgumentedRequestDone = fabricClient.BinaryReader.ReadSendArgumentedRequestDoneDto();
                        await Receive_SendArgumentedRequestDone_FromFabricAsync(sendArgumentedRequestDone, ct);
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
                        //await Receive_InvokeRequest_FromFabricAsync(invokeRequest, ct);
                        break;
                    case FabricHostToClientMessageEnum.InvokeResponse:
                        var invokeResponse = fabricClient.BinaryReader.ReadInvokeResponseDto();
                        //_ = Task.Run(async () => { await Receive_InvokeResponse_FromFabricAsync(invokeResponse, ct); }, ct);
                        await Receive_InvokeResponse_FromFabricAsync(invokeResponse, ct);
                        break;
                    case FabricHostToClientMessageEnum.InvokeResponseDone:
                        var requestId = fabricClient.BinaryReader.ReadRequestId();
                        //_ = Task.Run(async () => { await Receive_InvokeResponseDone_FromFabricAsync(requestId, ct); }, ct);
                        await Receive_InvokeResponseDone_FromFabricAsync(requestId, ct);
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

    public async Task Receive_SendRequest_FromFabricAsync(SendRequestDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Receive_SendRequest_FromFabricAsync({message})", message);

        var sseHosts = GetHosts(message.ServiceId, message.UserId, message.SessionId);
        foreach (var sseHost in sseHosts)
        {
            try
            {
                await sseHost.SendAsync(message, ct);
            }
            catch (TaskCanceledException)
            {
            }
        }
    }
    public async Task Receive_SendArgumentedRequest_FromFabricAsync(SendRequestDto message, CancellationToken ct)
    {
        var sseHosts = GetHosts(message.ServiceId, message.UserId, message.SessionId);
        foreach (var sseHost in sseHosts)
        {
            try
            {
                await sseHost.SendArgumentedAsync(message, ct);
            }
            catch (TaskCanceledException)
            {
            }
        }

        await fabricClient.Sender.Send_SendArgumentedRequestDone_ToFabricAsync(
            new SendArgumentedRequestDoneDto { RequestId = message.RequestId },
            ct);
    }
    public async Task Receive_SendArgumentedRequestDone_FromFabricAsync(SendArgumentedRequestDoneDto done, CancellationToken ct)
    {
        await fabricClient.Receive_SendArgumentedRequestDone_FromFabricAsync(done);
    }
    public async Task Receive_InvokeArgumentRequest_FromFabricAsync(InvokeArgumentRequestDto message, CancellationToken ct)
    {
        await fabricClient.TryHandleInvokeArgumentRequestAsync(message, ct);
    }
    public async Task Receive_InvokeArgumentResponse_FromFabricAsync(InvokeArgumentResponseDto message, CancellationToken ct)
    {
        var sseHosts = GetHosts(message.RequestId);
        foreach (var sseHost in sseHosts)
            await sseHost.SendArgumentResponseAsync(message, ct);
    }
    public async Task Receive_InvokeRequest_FromFabricAsync(InvokeRequestDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Receive_InvokeRequest_FromFabricAsync({message})", message);

        var sseHosts = GetHosts(message.ServiceId, message.UserId, message.SessionId);
        foreach (var sseHost in sseHosts)
        {
            try
            {
                var responses = sseHost.InvokeAsync(message, ct);
                await foreach (var response in responses)
                    await fabricClient.Sender.Send_InvokeResponse_ToFabricAsync(response, ct);

                await fabricClient.Sender.Send_InvokeResponseDone_ToFabricAsync(message.RequestId, ct);
            }
            catch (TaskCanceledException)
            {
            }
        }
    }
    public async Task Receive_InvokeResponse_FromFabricAsync(InvokeResponseDto response, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Receive_InvokeResponse_FromFabricAsync({response})", response);

        await fabricClient.Receive_InvokeResponse_FromFabricAsync(response);
    }
    private async Task Receive_InvokeResponseDone_FromFabricAsync(RequestId requestId, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Receive_InvokeResponseDone_FromFabricAsync({requestId})", requestId);

        await fabricClient.Receive_InvokeResponseDone_FromFabricAsync(requestId);
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
    private async Task Receive_GetSessionResponse_FromFabricAsync(SendGetSessionCookieDataResponseDto getSessionResponse, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Receive_GetSessionResponse_FromFabricAsync({getSessionResponse})", getSessionResponse);

        await fabricClient.Receive_GetSessionResponse_FromFabricAsync(getSessionResponse);
    }

    private IEnumerable<ISseHost> GetHosts(ServiceId serviceId, UserId? userId, SessionId? sessionId)
    {
        if (fabricClient.Services.TryGetValue(serviceId, out var hubHosts) == false)
            return [];

        return hubHosts.Values
            .Where(sseHost =>
                // Mogelijkheid 1: Naar iedereen: Beide null
                (sessionId == null && userId == null) ||
                // Mogelijkheid 2: Naar session: Session not null
                (sessionId != null && sseHost.SessionId == sessionId) ||
                // Mogelijkheid 3: Naar user: User not null
                (userId != null && sseHost.UserId == userId));
    }

    private IEnumerable<ISseHost> GetHosts(RequestId requestId)
    {
        return fabricClient.Services.Values.SelectMany(a => a.Values)
            .Where(a => a.HasRequest(requestId));
    }
}
