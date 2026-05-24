using gAPI.CodeGen.Backend.Generators.Shared.Dtos;
using gAPI.CodeGen.Backend.Models;
using gAPI.CodeGen.Backend.Models.Entities;
using System.Text;

namespace gAPI.CodeGen.Backend.Generators;

public class BaseGenerator : SharedReference
{
    public DirectoryInfo? Directory { get; protected set; }
    //public string? Namespace { get; protected set; }
    public string? FileName { get; protected set; }
    //public string? Name { get; protected set; }
    public string? Code { get; protected set; }
    private List<string> Namespaces { get; set; } = [];


    //public void Reg(CrudlType? dto)
    //{
    //    if (dto?.RealType?.Namespace != null)
    //        Namespaces.Add(dto.RealType.Namespace);
    //}
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
    //public void Reg(ServiceInterface? type)
    //{
    //    if (type?.Namespace != null)
    //        Namespaces.Add(type.Namespace);
    //}
    public void Reg(DbContext? type)
    {
        if (type?.Namespace != null)
            Namespaces.Add(type.Namespace);
    }
    public void Reg(Type? type)
    {
        if (type?.Namespace != null)
            Namespaces.Add(type.Namespace);
    }
    public void Reg(SharedReference? generator)
    {
        if (generator?.Namespace != null)
            Namespaces.Add(generator.Namespace);
    }
    public void Reg(EntityProperty? entityProperty)
    {
        if (entityProperty?.Type.Namespace != null)
            Namespaces.Add(entityProperty.Type.Namespace);
    }
    public void Reg(Entity? entity)
    {
        if (entity?.Type.Namespace != null)
            Namespaces.Add(entity.Type.Namespace);
    }
    public void Reg(DtoGenerator? entity)
    {
        if (entity?.Namespace != null)
            Namespaces.Add(entity.Namespace);
    }

    public void UnReg(string @namespace)
    {
        Namespaces.RemoveAll(a => a == @namespace);
    }

    //public string FullName => $"{Namespace}.{Name}";

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
        return code;
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
            code += $@"
@using {name}";
        }
        return code;
    }

    public void Save(bool overwrite = true)
    {
        //overwrite = true;
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
            File.WriteAllText(filePath, Code, Encoding.UTF8);
        }
    }
    public static string GetFolderPath(string huidige, string target)
    {
        var folder = string.Empty;
        if (target.StartsWith(huidige))
        {
            // dieper
            folder = string.Concat("./", target.AsSpan(huidige.Length));
        }
        else
        {
            // via root
            var split1 = huidige.Split('/');
            var split2 = target.Split('/');

            int i;
            for (i = split1.Length; i > 0; i--)
            {
                if (split2.Length >= i)
                {
                    var path1 = string.Join("/", split1.Take(i));
                    var path2 = string.Join("/", split2.Take(i));
                    if (path1 == path2)
                    {
                        break;
                    }
                }
                folder += "../";
            }

            foreach (var item in split2.Skip(i))
            {
                folder += item + "/";
            }
        }

        return folder;
    }

}