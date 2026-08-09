namespace gAPI.Llm.Client.Models;

public record Model(
    string Name,
    long? MemorySize = null,
    int? MaxTokenSize = null,
    DateTime? LastModified = null);