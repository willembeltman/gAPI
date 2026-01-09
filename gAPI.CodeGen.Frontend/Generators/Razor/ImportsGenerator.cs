using gAPI.CodeGen.Frontend.Configs;

namespace gAPI.CodeGen.Frontend.Generators.Razor;

public class ImportsGenerator : BaseGenerator
{
    public ImportsGenerator(FrontendConfig config)
    {
        Config = config;

        Name = "_Imports";

        Directory = config.RootDirectory;
        Namespace = config.RootNamespace;

        FileName = $"_Imports.razor";
    }

    public FrontendConfig Config { get; }

    internal void GenerateCode()
    {
        Reg("Microsoft.AspNetCore.Components.Web");
        Reg("Microsoft.AspNetCore.Components.Forms");
        Reg("Microsoft.AspNetCore.Components.Routing");
        Reg("Microsoft.AspNetCore.Components.Authorization");
        Reg(Config.ComponentsNamespace);
        //Reg(Config.HelpersNamespace);

        Code = GetRazorNamespacesCode();

        Save();
    }
}
