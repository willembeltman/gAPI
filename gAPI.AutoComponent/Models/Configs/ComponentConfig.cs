namespace gAPI.AutoComponent.Models.Configs;

public class ComponentConfig
{
    public string[] BaseNamespaces { get; set; } = new string[0];
    public NamespaceConfig Clients_Destination { get; set; } = new NamespaceConfig();
    public NamespaceConfig Components_Destination { get; set; } = new NamespaceConfig();
    public NamespaceConfig Helpers_Destination { get; set; } = new NamespaceConfig();
    public NamespaceConfig Authentication_Destination { get; set; } = new NamespaceConfig();
}