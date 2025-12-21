using gAPI.AutoComponent.Interfaces;

namespace gAPI.CodeGen.Frontend.Models.ServiceModels
{
    public class Client : ISharedReference
    {
        public Client(Interface @interface, Type namedTypeSymbol)
        {
            Interface = @interface;
            Type = namedTypeSymbol;

            Name = Type.Name;
            FullName = Type.FullName;
            Namespace = Type.Namespace;
        }

        public Interface Interface { get; }
        public Type Type { get; }
        public string Name { get; }
        public string? FullName { get; }
        public string? Namespace { get; }
    }
}