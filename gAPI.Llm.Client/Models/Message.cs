using gAPI.Llm.Client.Enums;

namespace gAPI.Llm.Client.Models;

public record Message(
    Role Role,
    string? ToolCallId,
    string? Content,
    string? Thinking,
    ToolCall[]? ToolCalls);
