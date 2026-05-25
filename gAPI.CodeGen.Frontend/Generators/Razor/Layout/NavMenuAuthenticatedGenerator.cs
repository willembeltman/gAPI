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
        Code = string.Join("\r\n\r\n", Generator.Cruds
            .Select(a => a.IndexViewGenerator)
            .Where(a => a.CrudType.IsEntryPoint && a.CrudType.ListMethod != null && a.CrudType.IsJunction == false)
            .Select(a => $@"<div class=""nav-item px-3"">
    <NavLink class=""nav-link"" href=""{a.CrudType.Name!.ToMultiple().ToLower()}"">
        <span class=""bi bi-list-nested-nav-menu"" aria-hidden=""true""></span> {a.CrudType.Name!.ToMultiple()}
    </NavLink>
</div>")
            .Concat(
                Generator.PageIndexes!
                    .Where(a => a.Pages.Any(b => b.IsAuthorized))
                    .Select(a => $@"<div class=""nav-item px-3"">
    <NavLink class=""nav-link"" href=""{a.Route}"">
        <span class=""bi bi-list-nested-nav-menu"" aria-hidden=""true""></span> {a.Title}
    </NavLink>
</div>"))
            .Concat(
                Generator.RootPages!
                    .Where(a => a.IsAuthorized)
                    .Select(a => $@"<div class=""nav-item px-3"">
    <NavLink class=""nav-link"" href=""{a.Route}"">
        <span class=""bi bi-list-nested-nav-menu"" aria-hidden=""true""></span> {a.Name}
    </NavLink>
</div>")));

        Save();
    }

}