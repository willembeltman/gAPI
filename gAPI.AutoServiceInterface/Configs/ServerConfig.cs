namespace gAPI.AutoServiceInterface.Configs
{
    internal class ServerConfig
    {
        public string[] BaseNamespaces { get; set; }
        // public string[] ServiceInterfaceNamespaces { get; set; }

        public NamespaceConfig Controllers_Destination { get; set; }
        public NamespaceConfig AddAutoServiceInterfaceServices_Destination { get; set; }
    }
}