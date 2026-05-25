using gAPI.AutoPage.Helpers;
using gAPI.AutoPage.Interfaces;
using System.Linq;

namespace gAPI.AutoPage.Generators.Layout;

public class NavMenuNotAuthenticatedGenerator : BaseGenerator
{
    public NavMenuNotAuthenticatedGenerator(
        IContext context,
        IBaseGenerator imports,
        string directory,
        string @namespace,
        bool makeAuto = false)
    {
        Context = context;
        Imports = imports;
        Directory = directory;
        Namespace = @namespace;

        Name = (makeAuto ? "Auto" : "") + "NavMenuNotAuthenticated";
        FileName = $"{Name}.razor";
    }

    public IContext Context { get; }
    public IBaseGenerator Imports { get; }

    public void GenerateCode()
    {
        Imports.Reg("Microsoft.AspNetCore.Components");
        Imports.Reg("Microsoft.AspNetCore.Components.Forms");
        Imports.Reg("Microsoft.AspNetCore.Components.Web");

        Code = string.Join("\r\n\r\n", Context.Cruds
            .Where(a => a.IsAuthorized == false && a.IsEntryPoint && a.ListMethod != null && a.IsJunction == false)
            .Select(a => $@"<div class=""nav-item px-3"">
    <NavLink class=""nav-link"" href=""{a.Name!.ToMultiple().ToLower()}"">
        <span class=""bi bi-list-nested-nav-menu"" aria-hidden=""true""></span> {a.Name!.ToMultiple()}
    </NavLink>
</div>")
            .Concat(Context.PageIndexes
            .Where(a => a.Pages.Any(b => b.IsNotAuthorized || (b.IsAuthorized == false && b.IsNotAuthorized == false)))
            .Select(a => $@"<div class=""nav-item px-3"">
    <NavLink class=""nav-link"" href=""{a.Route}"">
        <span class=""bi bi-list-nested-nav-menu"" aria-hidden=""true""></span> {a.Title}
    </NavLink>
</div>"))
            .Concat(Context.RootPages
            .Where(a => a.IsNotAuthorized || (a.IsAuthorized == false && a.IsNotAuthorized == false))
            .Select(a => $@"<div class=""nav-item px-3"">
    <NavLink class=""nav-link"" href=""{a.Route}"">
        <span class=""bi bi-list-nested-nav-menu"" aria-hidden=""true""></span> {a.Name}
    </NavLink>
</div>")));
    }

}