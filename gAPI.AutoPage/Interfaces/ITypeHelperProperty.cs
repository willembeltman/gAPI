using gAPI.AutoPage.Helpers;
using System;

namespace gAPI.AutoPage.Interfaces
{
    public interface ITypeHelperProperty
    {
        ITypeHelper Type { get; }
        string Name { get; }
        string Title { get; }
        bool IsPassword { get; }
        bool IsForeignKey { get; }
        TypeHelper? IsForeignKeyType { get; }
    }
}