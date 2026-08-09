using gAPI.Llm.Client.Models;

namespace gAPI.Llm.Client.Interfaces;

public interface ILlmClient : IDisposable
{
    bool Initialized { get; }

    Task<Model[]> GetModels(CancellationToken ct = default);
    Task InitializeModelAsync(Model model, CancellationToken ct = default);
    Task<LlmResponse> ChatAsync(Model model, LlmRequest apiCall, CancellationToken ct = default, LlmOptions? options = null);

    string CreateMessagesJson(Message[] messages);
    string CreateRequestJson(Model model, LlmRequest apiCall, LlmOptions? options = null);
    string CreateToolsJson(Tool[] tools);
}