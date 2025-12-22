namespace gAPI.AutoService.Configs
{
    internal class ServerConfig
    {
        public string[] BaseNamespaces { get; set; }
        // public string[] ServiceInterfaceNamespaces { get; set; }

        public NamespaceConfig Controllers_Destination { get; set; }
        public NamespaceConfig AddAutoServiceServices_Destination { get; set; }
    }
}