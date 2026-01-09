namespace gAPI.AutoSseClient.Configs;

internal class ClientConfig
{
    public string[] BaseNamespaces { get; set; }

    public NamespaceConfig HubClients_Destination { get; set; }
    public NamespaceConfig AddAutoSseClientServices_Destination { get; set; }
}