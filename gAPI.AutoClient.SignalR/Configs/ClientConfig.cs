namespace gAPI.AutoClient.SignalR.Configs
{
    internal class ClientConfig
    {
        public string[] BaseNamespaces { get; set; }

        public NamespaceConfig HubClients_Destination { get; set; }
        public NamespaceConfig AddAutoHubClientServices_Destination { get; set; }
    }
}