using gAPI.AutoPage.Helpers;
using gAPI.AutoPage.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace gAPI.AutoPage.Generators.Pages;

public class IndexGenerator : BaseGenerator, IPageIndex
{
    public IndexGenerator(
        string routePath,
        IEnumerable<PageGenerator> pages,
        IBaseGenerator imports,
        string directory,
        string @namespace)
    {
        RoutePath = routePath;
        Imports = imports;
        Pages = pages.ToArray();

        Name = "Index";
        NameParts = routePath.Split(['/'], StringSplitOptions.RemoveEmptyEntries);
        NamespaceParts = (@namespace ?? "").Split(['.']);
        Namespace = string.Join(".", [.. NamespaceParts, .. NameParts]);
        Route = $"/{string.Join("/", [.. NameParts, Name])}";
        FileName = $"{Name}.razor";

        var fullDirectory = directory;
        foreach (var path in NameParts)
        {
            fullDirectory = Path.Combine(fullDirectory, path);
        }
        Directory = fullDirectory;

        Title = NameParts.LastOrDefault()?.ToNameCase()
            ?? Pages.First().Namespace?.Split(['.']).LastOrDefault()?.ToNameCase()
            ?? Name;
    }

    public string RoutePath { get; }
    public IBaseGenerator Imports { get; }
    public PageGenerator[] Pages { get; }
    public string Route { get; }
    public string[] NameParts { get; }
    public string[] NamespaceParts { get; }
    public string Title { get; }

    IEnumerable<IPage> IPageIndex.Pages => Pages;

    public void GenerateCode()
    {
        Imports.Reg("Microsoft.AspNetCore.Components");
        Imports.Reg("Microsoft.AspNetCore.Components.Forms");
        Imports.Reg("Microsoft.AspNetCore.Components.Web");

        Code = $@"@page ""{Route}""

<PageTitle>{Title}</PageTitle>
<h3>{Title}</h3>

<AuthorizeView>
    <Authorized Context=""_"">
{(Pages.Any(a => a.IsAuthorized)
? string.Join("", Pages.Where(a => a.IsAuthorized).Select(a => $@"
        <div class=""px-3"">
            <NavLink class=""nav-link"" href=""{a.Route}"">
                <span aria-hidden=""true""></span> {a.Name}
            </NavLink>
        </div>"))
: $@"
        <RedirectToHome />")}

    </Authorized>
    <NotAuthorized Context=""_"">
{(Pages.Any(a=> a.IsAuthorized == false) 
? string.Join("", Pages.Where(a => a.IsAuthorized == false).Select(a => $@"
        <div class=""px-3"">
            <NavLink class=""nav-link"" href=""{a.Route}"">
                <span aria-hidden=""true""></span> {a.Name}
            </NavLink>
        </div>"))
: $@"
        <RedirectToLogin />")}

    </NotAuthorized>
</AuthorizeView>
";
    }
}