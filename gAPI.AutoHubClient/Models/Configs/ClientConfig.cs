namespace gAPI.AutoHubClient.Models.Configs;

internal class ClientConfig
{
    public string[] BaseNamespaces { get; set; } = [];

    public NamespaceConfig HubClients_Destination { get; set; } = new();
    public NamespaceConfig AddAutoHubClientServices_Destination { get; set; } = new();
}