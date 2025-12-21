namespace gAPI.AutoComponent.Interfaces;

public interface ICrudlProperty
{
    string TypeSimpleName { get; }
    string Name { get; }
    bool IsNullable { get; }
    bool IsStateManaged { get; }
    bool IsNumber { get; }
    bool IsDateTime { get; }
    bool IsCheckbox { get; }
    bool IsStorageFile { get; }
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
    bool IsName { get; }
}