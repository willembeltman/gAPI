namespace gAPI.Llm.Client.Models;

public record ToolCall(
    string Id,
    ToolCallFunction Function);
