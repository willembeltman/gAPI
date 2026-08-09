namespace gAPI.Llm.Client.Models;

public record LlmRequest(
    Message[] Messages,
    Tool[] Tools);
