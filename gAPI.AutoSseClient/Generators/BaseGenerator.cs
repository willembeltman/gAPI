using gAPI.AutoSseClient.Models;
using System.Collections.Generic;
using System.Linq;

namespace gAPI.AutoSseClient.Generators;


internal class BaseGenerator : SharedReference
{
    public string Directory { get; protected set; } = string.Empty;
    public string FileName { get; protected set; } = string.Empty;
    public string Code { get; protected set; } = string.Empty;
    private List<string> Namespaces { get; set; } = [];


    internal void Reg(string @namespace)
    {
        if (@namespace != null)
            Namespaces.Add(@namespace);
    }
    internal void RegRange(IEnumerable<string> namespaces)
    {
        if (namespaces != null)
            foreach (var @namespace in namespaces)
                if (@namespace != null)
                    Namespaces.Add(@namespace);
    }
    internal void Reg(Interface type)
    {
        if (type?.Namespace != null)
            Namespaces.Add(type.Namespace);
    }
    internal void Reg(BaseGenerator generator)
    {
        if (generator?.Namespace != null)
            Namespaces.Add(generator.Namespace);
    }
    internal void Reg(SharedReference generator)
    {
        if (generator?.Namespace != null)
            Namespaces.Add(generator.Namespace);
    }

    internal void UnReg(string @namespace)
    {
        Namespaces.RemoveAll(a => a == @namespace);
    }


    internal string GetNamespacesCode()
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
    internal string GetRazorNamespacesCode()
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

    internal static string GetFolderPath(string huidige, string target)
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