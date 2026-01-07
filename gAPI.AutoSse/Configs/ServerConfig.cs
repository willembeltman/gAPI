namespace gAPI.AutoSse.Configs
{
    internal class ServerConfig
    {
        public string[] BaseNamespaces { get; set; }

        public NamespaceConfig Sses_Destination { get; set; }
        public NamespaceConfig SseServices_Destination { get; set; }
        public NamespaceConfig AddAutoSseServices_Destination { get; set; }
    }
}