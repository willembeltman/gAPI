namespace gAPI.AutoSse.Configs;

internal class ServerConfig
{
    public string[] BaseNamespaces { get; set; }

    public NamespaceConfig SseContextInterfaces_Destination { get; set; }
    public NamespaceConfig SseServiceInterfaces_Destination { get; set; }
    public NamespaceConfig SseContexts_Destination { get; set; }
    public NamespaceConfig SseServices_Destination { get; set; }
    public NamespaceConfig AddAutoSseServices_Destination { get; set; }
    public NamespaceConfig SseHostController_Destination { get; internal set; }
}