using gAPI.AutoComponent.Interfaces;

namespace gAPI.CodeGen.Frontend.Generators;

public class BaseGenerator : IBaseGenerator
{
    public DirectoryInfo? Directory { get; set; }
    public string? Namespace { get; set; }
    public string? FileName { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    private List<string> Namespaces { get; set; } = [];

    public string FullName => $"{Namespace}.{Name}";
    string IBaseGenerator.Directory => Directory!.FullName;

    public void Reg(ISharedReference? sharedReference)
    {
        if (sharedReference?.Namespace != null)
            Namespaces.Add(sharedReference.Namespace);
    }
    public void Reg(string? @namespace)
    {
        if (@namespace != null)
            Namespaces.Add(@namespace);
    }
    public void RegRange(IEnumerable<string?>? namespaces)
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

        Namespaces = [.. Namespaces
            .GroupBy(a => a)
            .Select(a => a.Key)
            .Where(a => a != "System" && a != Namespace)
            .OrderBy(a => a)];

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

        Namespaces = [.. Namespaces
            .GroupBy(a => a)
            .Select(a => a.Key)
            .Where(a => a != "System")
            .OrderBy(a => a)];

        var code = string.Empty;
        foreach (var name in Namespaces)
        {
            code += $@"@using {name}
";
        }
        return code;
    }

    public void Save(bool overwrite = true)
    {
        //Console.WriteLine(Code);

        if (Directory == null || string.IsNullOrWhiteSpace(FileName) || Code == null)
        {
            throw new InvalidOperationException("Directory, Name, and Code must be set before saving.");
        }
        var filePath = Path.Combine(Directory.FullName, FileName);

        Console.WriteLine(filePath);

        var fileInfo = new FileInfo(filePath);
        if (overwrite || !fileInfo.Exists)
        {
            if (!fileInfo.Directory!.Exists)
            {
                fileInfo.Directory.Create();
            }
            File.WriteAllText(filePath, Code);
        }
    }
}