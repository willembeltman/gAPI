namespace gAPI.Llm.Client.Models;

public record ToolCallFunction(
    string Name,
    ToolCallFunctionArguments Arguments);