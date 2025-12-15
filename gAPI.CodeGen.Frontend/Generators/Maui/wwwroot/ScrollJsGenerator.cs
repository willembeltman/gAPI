using gAPI.CodeGen.Frontend.Configs;

namespace gAPI.CodeGen.Frontend.Generators.Maui.wwwroot;

public class ScrollJsGenerator : BaseGenerator
{
    public ScrollJsGenerator(FrontendConfig config)
    {
        if (config.BlazorMauiDirectory == null) return;

        var dir = config.BlazorMauiDirectory.GetDirectories()
            .FirstOrDefault(a => a.Name == "wwwroot");

        if (dir == null)
            throw new DirectoryNotFoundException("wwwroot directory not found in Blazor Maui project.");

        Directory = dir;
        FileName = "scroll.js";
    }

    public void GenerateCode()
    {
        Code = @"
window.registerScrollListener = (dotNetHelper) => {
    window.onscroll = () => {
        const scrollTop = window.scrollY || document.documentElement.scrollTop;
        const scrollHeight = document.documentElement.scrollHeight;
        const clientHeight = document.documentElement.clientHeight;

        if (scrollHeight - scrollTop - clientHeight < 100) {
            dotNetHelper.invokeMethodAsync('OnNearBottom');
        }
    };
};

window.removeScrollListener = () => {
    window.onscroll = null;
};
";
        Save();
    }
}
