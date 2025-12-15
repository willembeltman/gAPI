using Microsoft.CodeAnalysis;

namespace gAPI.CodeGen.Frontend.Models.ServiceModels
{
    public class EnumDto
    {
        public EnumDto(Type type)
        {
            Type = type;

            Name = Type.Name;
            FullName = Type.FullName;
            Namespace = Type.Namespace;

            Choices = Type
                .GetMembers()
                .OfType<IFieldSymbol>()
                .Where(f => f.ConstantValue != null)
                .Select(fieldSymbol => new EnumChoice(this, fieldSymbol))
                .ToArray();
        }

        public Type Type { get; }
        public string Name { get; }
        public string? FullName { get; }
        public string? Namespace { get; }
        public EnumChoice[] Choices { get; }
    }
}