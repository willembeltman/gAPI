namespace gAPI.AutoHub.Configs
{
    internal class ServerConfig
    {
        public string[] BaseNamespaces { get; set; }

        public NamespaceConfig Hubs_Destination { get; set; }
        public NamespaceConfig AddAutoHubServices_Destination { get; set; }
    }
}