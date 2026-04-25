namespace gAPI.AutoComponent.Interfaces;

public interface ITypeDigger : ISharedReference
{
    ITypeHelper Type { get; }
    ITypeHelper StartType { get; }
}
