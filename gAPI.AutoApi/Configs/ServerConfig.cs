namespace gAPI.AutoApi.Configs
{
    internal class ServerConfig
    {
        public string[] BaseNamespaces { get; set; }
        // public string[] ServiceInterfaceNamespaces { get; set; }

        public NamespaceConfig Controllers_Destination { get; set; }
        public NamespaceConfig AddAutoApiServices_Destination { get; set; }
    }
}