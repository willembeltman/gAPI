using System.Reflection;

namespace gAPI.CodeGen.Frontend.Models.Configs;

public record FrontendConfig(
    Assembly[] Assemblies,
    string[] BaseNamespaces,

    DirectoryInfo RootDirectory,
    string? RootNamespace,
    DirectoryInfo? BlazorMauiDirectory,
    string? BlazorMauiNamespace,
    DirectoryInfo? BlazorWebassemblyDirectory,
    string? BlazorWebassemblyServiceNamespace,

    DirectoryInfo LayoutDirectory,
    string? LayoutNamespace,

    DirectoryInfo PagesDirectory,
    string? PagesNamespace,

    DirectoryInfo? ComponentsDirectory,
    string? ComponentsNamespace,
    bool GenerateIsPage = true
    )
{
    //public Assembly[] Assemblies { get; } = AssembliesToSearch;
    //public string[] BaseNamespaces { get; set; } = BaseNamespacesToFilter;

    //public DirectoryInfo RootDirectory { get; } = RootDirectory;
    //public string? RootNamespace { get; } = RootNamespace;

    //public DirectoryInfo PagesDirectory { get; } = PagesDirectory;
    //public string? PagesNamespace { get; } = PagesNamespace;

    //public DirectoryInfo LayoutDirectory { get; } = LayoutDirectory;
    //public string? LayoutNamespace { get; } = LayoutNamespace;

    //public DirectoryInfo? BlazorMauiDirectory { get; } = BlazorMauiDirectory;
    //public string? BlazorMauiNamespace { get; } = BlazorMauiNamespace;

    //public DirectoryInfo? BlazorWebassemblyDirectory { get; } = BlazorWebassemblyDirectory;
    //public string? BlazorWebassemblyServiceNamespace { get; } = BlazorWebassemblyServiceNamespace;

    //public DirectoryInfo? ComponentsDirectory { get; } = ComponentsDirectory;
    //public string? ComponentsNamespace { get; } = ComponentsNamespace;
}
