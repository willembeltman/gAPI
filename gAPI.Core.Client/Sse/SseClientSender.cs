using gAPI.Core.Client.Interfaces;
using gAPI.Core.Dtos;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace gAPI.Core.Client.Sse;

public class SseClientSender(SseClient sseClient)
{
    public SseClient SseClient { get; } = sseClient;
    public IClientAuthenticatedHttpClient HttpClient => SseClient.HttpClient;

    public async Task Send_InvokeRequestDone_ToServerAsync(InvokeRequestDoneClientDto message, CancellationToken ct)
    {
        try
        {
            var content = new MultipartFormDataContent();
            content.Write("message", message);

            using var response = await HttpClient.PostAsync("/SseServiceSubscription/InvokeRequestDone", content, ct);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            throw;
        }
    }
    public async Task Send_StreamingResponse_ToServerAsync(StreamingResponseClientDto message, CancellationToken ct)
    {
        try
        {
            var content = new MultipartFormDataContent();
            content.Write("message", message);

            using var response = await HttpClient.PostAsync("/SseServiceSubscription/StreamingResponse", content, ct);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            throw;
        }
    }
    public async Task Send_FabricStreamingResponse_ToServerAsync(StreamingResponseClientDto message, CancellationToken ct)
    {
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
            throw;
        }
    }
}