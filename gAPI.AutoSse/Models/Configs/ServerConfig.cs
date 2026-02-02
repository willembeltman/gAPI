namespace gAPI.AutoSse.Models.Configs;

internal class ServerConfig
{
    public string[] BaseNamespaces { get; set; } = [];

    public NamespaceConfig SseContextInterfaces_Destination { get; set; } = new();
    public NamespaceConfig SseServiceInterfaces_Destination { get; set; } = new();
    public NamespaceConfig SseContexts_Destination { get; set; } = new();
    public NamespaceConfig SseServices_Destination { get; set; } = new();
    public NamespaceConfig AddAutoSseServices_Destination { get; set; } = new();
}