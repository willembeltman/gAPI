namespace gAPI.AutoPage.Models.Configs;

public class PageConfig
{
    public string[] BaseNamespaces { get; set; } = [];
    public NamespaceConfig Pages_Destination { get; set; } = new NamespaceConfig();
    public NamespaceConfig Layout_Destination { get; set; } = new NamespaceConfig();
    public NamespaceConfig Components_Destination { get; set; } = new NamespaceConfig();
}