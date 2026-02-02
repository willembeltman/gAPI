namespace gAPI.AutoApiClient.Models.Configs;

public class ClientConfig
{
    public string[] BaseNamespaces { get; set; } = [];
    public NamespaceConfig Clients_Destination { get; set; } = new();
    public NamespaceConfig Helpers_Destination { get; set; } = new();
}