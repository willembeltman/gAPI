namespace gAPI.AutoComponents.Interfaces;

public interface ISharedReference
{
    string Name { get; }
    string? Namespace { get; }
    string? FullName { get; }
}
