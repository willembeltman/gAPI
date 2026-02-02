using System.Collections.Generic;

namespace gAPI.AutoPage.Interfaces;

public interface IBaseGenerator : ISharedReference
{
    string? Code { get; }
    string Directory { get; }
    string? FileName { get; }

    string GetNamespacesCode();
    string GetRazorNamespacesCode();
    void Reg(ISharedReference? reference);
    void Reg(string? @namespace);
    void RegRange(IEnumerable<string?> namespaces);
    void UnReg(string @namespace);
}