using gAPI.AutoPage.Models.ServiceModels;

namespace gAPI.AutoPage.Interfaces;

public interface ITypeHelperProperty
{
    ITypeHelper Type { get; }
    ITypeHelper? IsForeignKeyType { get; }
    string Name { get; }
    string Title { get; }
    bool IsPassword { get; }
    bool IsForeignKey { get; }
    bool IsReadOnly { get; }
    bool IsForeignName { get; }
    bool IsStateManaged { get; }
    bool IsImmutable { get; }
    bool IsStorageFileUrlProperty { get; }
    bool IsKey { get; }
    bool IsName { get; }
    ITypeHelperPropertyAttribute[] GetAttributes();
}