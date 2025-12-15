using gAPI.CodeGen.Frontend.Helpers;

namespace gAPI.CodeGen.Frontend.Generators.Razor.Layout;

public class NavMenuAuthenticatedGenerator : BaseGenerator
{
    public NavMenuAuthenticatedGenerator(FrontendGenerator generator)
    {
        Generator = generator;
        Namespace = generator.Config.LayoutNamespace;

        Directory = generator.Config.LayoutDirectory;
        Namespace = $"{generator.Config.LayoutNamespace}";

        Name = "NavMenuAuthenticated";
        FileName = $"{Name}.razor";
    }

    public FrontendGenerator Generator { get; }

    public void GenerateCode()
    {
        Code = string.Join("\r\n\r\n", Generator.CrudlGenerators
            .Select(a => a.IndexViewGenerator)
            .Where(a => a.CrudlType.IsEntryPoint && a.CrudlType.ListMethod != null && a.CrudlType.IsJunction == false)
            .Select(a => $@"
<div class=""nav-item px-3"">
    <NavLink class=""nav-link"" href=""{a.CrudlType.Name!.ToMultiple().ToLower()}"">
        <span class=""bi bi-list-nested-nav-menu"" aria-hidden=""true""></span> {a.CrudlType.Name!.ToMultiple()}
    </NavLink>
</div>"));

        Save();
    }

}