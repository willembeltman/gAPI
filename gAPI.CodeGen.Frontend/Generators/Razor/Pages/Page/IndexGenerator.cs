using gAPI.CodeGen.Frontend.Helpers;
using gAPI.CodeGen.Frontend.Models.Configs;

namespace gAPI.CodeGen.Frontend.Generators.Razor.Pages.Page;

public class IndexGenerator : BaseGenerator
{
    public IndexGenerator(
        string routePath,
        PageGenerator[] pages,
        FrontendConfig clientConfig,
        ImportsGenerator imports)
    {
        RoutePath = routePath;
        Imports = imports;
        Pages = pages;

        Name = "Index";
        NameParts = routePath.Split(['/'], StringSplitOptions.RemoveEmptyEntries);
        NamespaceParts = (clientConfig.PagesNamespace ?? "").Split(['.']);
        Namespace = string.Join(".", [.. NamespaceParts, .. NameParts]);
        Route = $"/{string.Join("/", [.. NameParts, Name])}";
        FileName = $"{Name}.razor";

        var fullDirectory = clientConfig.PagesDirectory.FullName;
        foreach (var path in NameParts)
        {
            fullDirectory = Path.Combine(fullDirectory, path);
        }
        Directory = new DirectoryInfo(fullDirectory);

        Title = NameParts.LastOrDefault()?.ToNameCase()
            ?? Pages.First().Namespace?.Split(['.']).LastOrDefault()?.ToNameCase()
            ?? Name;
    }

    public string RoutePath { get; }
    public ImportsGenerator Imports { get; }
    public PageGenerator[] Pages { get; }
    public string Route { get; }
    public string[] NameParts { get; }
    public string[] NamespaceParts { get; }
    public string Title { get; }

    public void GenerateCode()
    {
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
{(Pages.Any(a => a.IsAuthorized == false)
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

        Save(false);
    }
}