namespace gAPI.AutoComponent.Interfaces;

public interface ICrudlProperty
{
    string TypeSimpleName { get; }
    string Name { get; }
    bool IsName { get; }
    bool IsNullable { get; }
    bool IsStateManaged { get; }
    bool IsImmutable { get; }
    bool IsNumber { get; }
    bool IsDateTime { get; }
    bool IsCheckbox { get; }
    bool IsStorageFileUrlProperty { get; }
    bool IsEnum { get; }
    bool IsReadOnly { get; }
    bool IsForeignName { get; }
    bool IsKey { get; }
    ITypeHelper PropertyType { get; }
    ITypeDigger TypeDigger { get; }
    ICrudlType? ForeignKeyType { get; }
    ICrudlMethod? ListByMethod { get; }
    ICrudlMethod? ListMethod { get; }
    ICrudlProperty? ForeignKeyNameProperty { get; }
}