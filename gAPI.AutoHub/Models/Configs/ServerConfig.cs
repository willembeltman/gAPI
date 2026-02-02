namespace gAPI.AutoHub.Models.Configs;

internal class ServerConfig
{
    public string[] BaseNamespaces { get; set; } = [];

    public NamespaceConfig Hubs_Destination { get; set; } = new();
    public NamespaceConfig HubServices_Destination { get; set; } = new();
    public NamespaceConfig AddAutoHubServices_Destination { get; set; } = new();
}