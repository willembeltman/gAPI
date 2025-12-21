using gAPI.AutoComponent.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace gAPI.AutoComponent.Generators;

public class BaseGenerator(string directory, string @namespace) : IBaseGenerator
{
    public string Directory { get; set; } = directory;
    public string Namespace { get; set; } = @namespace;
    public string FileName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FullName => $"{Namespace}.{Name}";
    private List<string> Namespaces { get; set; } = [];

    public void Reg(ISharedReference? reference)
    {
        if (reference?.Namespace != null)
            Namespaces.Add(reference.Namespace);
    }
    public void Reg(string? @namespace)
    {
        if (@namespace != null)
            Namespaces.Add(@namespace);
    }
    public void RegRange(IEnumerable<string?> namespaces)
    {
        if (namespaces != null)
            foreach (var @namespace in namespaces)
                if (@namespace != null)
                    Namespaces.Add(@namespace);
    }

    public void UnReg(string @namespace)
    {
        Namespaces.RemoveAll(a => a == @namespace);
    }

    public string GetNamespacesCode()
    {
        if (Namespaces.Count == 0) return "";

        Namespaces = Namespaces
            .GroupBy(a => a)
            .Select(a => a.Key)
            .Where(a => a != "System" && a != Namespace)
            .OrderBy(a => a)
            .ToList();

        var code = string.Empty;
        foreach (var name in Namespaces)
        {
            code += $"using {name};" + Environment.NewLine;
        }
        return code + Environment.NewLine;
    }
    public string GetRazorNamespacesCode()
    {
        if (Namespaces.Count == 0) return "";

        Namespaces = Namespaces
            .GroupBy(a => a)
            .Select(a => a.Key)
            .Where(a => a != "System")
            .OrderBy(a => a)
            .ToList();

        var code = string.Empty;
        foreach (var name in Namespaces)
        {
            code += $@"
@using {name}";
        }
        return code;
    }

    public override string ToString() => FullName;
}