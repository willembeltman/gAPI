using gAPI.Core.Client.Interfaces;
using gAPI.Core.Dtos;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace gAPI.Core.Client.Sse;

public class SseClientSender(
    ILoggerFactory loggerFactory,
    SseClientSubscription sseClient)
{
    private readonly ILogger Logger = loggerFactory.CreateLogger<SseClientSender>();
    public SseClientSubscription SseClient { get; } = sseClient;
    public IClientAuthenticatedHttpClient HttpClient => SseClient.HttpClient;

    public async Task Send_SendRequestDone_ToServerAsync(SendRequestDoneClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Send_SendRequestDone_ToServerAsync({message})", message);
        try
        {
            var content = new MultipartFormDataContent();
            content.Write("message", message);

            using var response = await HttpClient.PostAsync("/SseServiceSubscription/SendRequestDone", content, ct);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Send_InvokeRequestDone_ToServerAsync({message})", message);
            throw;
        }
    }
    public async Task Send_InvokeRequestAck_ToServerAsync(InvokeRequestAckClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Send_InvokeRequestAck_ToServerAsync({message})", message);
        try
        {
            var content = new MultipartFormDataContent();
            content.Write("message", message);

            using var response = await HttpClient.PostAsync("/SseServiceSubscription/InvokeRequestAck", content, ct);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Send_InvokeRequestAck_ToServerAsync({message})", message);
            throw;
        }
    }
    public async Task Send_InvokeRequestDone_ToServerAsync(InvokeRequestDoneClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Send_InvokeRequestDone_ToServerAsync({message})", message);
        try
        {
            var content = new MultipartFormDataContent();
            content.Write("message", message);

            using var response = await HttpClient.PostAsync("/SseServiceSubscription/InvokeRequestDone", content, ct);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Send_InvokeRequestDone_ToServerAsync({message})", message);
            throw;
        }
    }
    public async Task Send_StreamingRequest_ToServerAsync(StreamingRequestClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Send_StreamingRequest_ToServerAsync({message})", message);
        try
        {
            var content = new MultipartFormDataContent();
            content.Write("message", message);

            using var response = await HttpClient.PostAsync("/SseServiceSubscription/StreamingRequest", content, ct);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Send_StreamingRequest_ToServerAsync({message})", message);
            throw;
        }
    }
    public async Task Send_FabricStreamingRequest_ToServerAsync(StreamingRequestClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Send_StreamingRequest_ToServerAsync({message})", message);
        try
        {
            var content = new MultipartFormDataContent();
            content.Write("message", message);

            using var response = await HttpClient.PostAsync("/SseServiceSubscription/FabricStreamingRequest", content, ct);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Send_StreamingRequest_ToServerAsync({message})", message);
            throw;
        }
    }
    public async Task Send_StreamingResponse_ToServerAsync(StreamingResponseClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Send_StreamingResponse_ToServerAsync({message})", message);
        try
        {
            var json = JsonSerializer.Serialize(message);
            var content = new MultipartFormDataContent
            {
                { new StringContent(json), $"messageJson" }
            };

            using var response = await HttpClient.PostAsync("/SseServiceSubscription/StreamingResponse", content, ct);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Send_StreamingResponse_ToServerAsync({message})", message);
            throw;
        }
    }

    public async Task Send_FabricStreamingResponse_ToServerAsync(StreamingResponseClientDto message, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Send_FabricStreamingResponse_ToServerAsync({message})", message);
        try
        {
            var json = JsonSerializer.Serialize(message);
            var content = new MultipartFormDataContent
            {
                { new StringContent(json), $"messageJson" }
            };

            using var response = await HttpClient.PostAsync("/SseServiceSubscription/FabricStreamingResponse", content, ct);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Send_FabricStreamingResponse_ToServerAsync({message})", message);
            throw;
        }
    }

}