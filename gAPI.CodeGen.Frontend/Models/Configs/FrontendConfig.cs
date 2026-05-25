using System.Reflection;

namespace gAPI.CodeGen.Frontend.Models.Configs;

public class FrontendConfig(
    Assembly[] AssembliesToSearch,
    string[] BaseNamespacesToFilter,
    DirectoryInfo RootDirectory,
    string? RootNamespace,
    DirectoryInfo PagesDirectory,
    string? PagesNamespace,
    DirectoryInfo LayoutDirectory,
    string? LayoutNamespace,
    DirectoryInfo? BlazorMauiDirectory,
    string? BlazorMauiNamespace,
    DirectoryInfo? BlazorWebassemblyDirectory,
    string? BlazorWebassemblyServiceNamespace,
    DirectoryInfo? ComponentsDirectory,
    string? ComponentsNamespace
    )
{
    public Assembly[] Assemblies { get; } = AssembliesToSearch;
    public string[] BaseNamespaces { get; set; } = BaseNamespacesToFilter;

    public DirectoryInfo RootDirectory { get; } = RootDirectory;
    public string? RootNamespace { get; } = RootNamespace;

    public DirectoryInfo PagesDirectory { get; } = PagesDirectory;
    public string? PagesNamespace { get; } = PagesNamespace;

    public DirectoryInfo LayoutDirectory { get; } = LayoutDirectory;
    public string? LayoutNamespace { get; } = LayoutNamespace;

    public DirectoryInfo? BlazorMauiDirectory { get; } = BlazorMauiDirectory;
    public string? BlazorMauiNamespace { get; } = BlazorMauiNamespace;

    public DirectoryInfo? BlazorWebassemblyDirectory { get; } = BlazorWebassemblyDirectory;
    public string? BlazorWebassemblyServiceNamespace { get; } = BlazorWebassemblyServiceNamespace;

    public DirectoryInfo? ComponentsDirectory { get; } = ComponentsDirectory;
    public string? ComponentsNamespace { get; } = ComponentsNamespace;
}
