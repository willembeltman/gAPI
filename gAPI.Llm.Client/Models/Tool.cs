namespace gAPI.Llm.Client.Models;

public record Tool(
    string Name,
    string Desciption,
    ToolParameter[] Parameters);
