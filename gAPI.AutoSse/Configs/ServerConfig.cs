namespace gAPI.AutoSse.Configs
{
    internal class ServerConfig
    {
        public string[] BaseNamespaces { get; set; }

        public NamespaceConfig Hubs_Destination { get; set; }
        public NamespaceConfig HubServices_Destination { get; set; }
        public NamespaceConfig AddAutoSseServices_Destination { get; set; }
    }
}