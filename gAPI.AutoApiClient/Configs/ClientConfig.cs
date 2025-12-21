namespace gAPI.AutoApiClient.Configs
{
    internal class ClientConfig
    {
        public string[] BaseNamespaces { get; set; }
        public NamespaceConfig Clients_Destination { get; set; }
        public NamespaceConfig Helpers_Destination { get; set; }
        //public NamespaceConfig Authentication_Destination { get; internal set; }
    }
}