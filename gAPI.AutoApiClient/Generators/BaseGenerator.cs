using gAPI.AutoApiClient.Models;
using System.Collections.Generic;
using System.Linq;

namespace gAPI.AutoApiClient.Generators;


public class BaseGenerator : SharedReference
{
    public string Directory { get; protected set; } = string.Empty;
    public string FileName { get; protected set; } = string.Empty;
    public string Code { get; protected set; } = string.Empty;
    //public string Name { get; protected set; }
    //public string Namespace { get; protected set; }
    //public string FullName => $"{Namespace}.{Name}";
    private List<string> Namespaces { get; set; } = new List<string>();


    public void Reg(string @namespace)
    {
        if (@namespace != null)
            Namespaces.Add(@namespace);
    }
    public void RegRange(IEnumerable<string> namespaces)
    {
        if (namespaces != null)
            foreach (var @namespace in namespaces)
                if (@namespace != null)
                    Namespaces.Add(@namespace);
    }
    public void Reg(Interface type)
    {
        if (type?.Namespace != null)
            Namespaces.Add(type.Namespace);
    }
    public void Reg(BaseGenerator generator)
    {
        if (generator?.Namespace != null)
            Namespaces.Add(generator.Namespace);
    }
    public void Reg(SharedReference generator)
    {
        if (generator?.Namespace != null)
            Namespaces.Add(generator.Namespace);
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
        return code;
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

    public static string GetFolderPath(string huidige, string target)
    {
        var folder = string.Empty;
        if (target.StartsWith(huidige))
        {
            // dieper
            folder = "./" + target.Substring(huidige.Length);
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