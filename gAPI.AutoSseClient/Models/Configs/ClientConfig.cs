namespace gAPI.AutoSseClient.Models.Configs;

internal class ClientConfig
{
    public string[] BaseNamespaces { get; set; } = [];

    public NamespaceConfig HubClients_Destination { get; set; } = new();
    public NamespaceConfig AddAutoSseClientServices_Destination { get; set; } = new();
}