using gAPI.Attributes;
using gAPI.CodeGen.Frontend.Contexts;
using gAPI.CodeGen.Frontend.Helpers;
using gAPI.Interfaces;
using Microsoft.CodeAnalysis;
using System.Reflection;

namespace gAPI.CodeGen.Frontend.Models.ServiceModels
{
    public class Dto
    {
        public Dto(ServiceContext dataModel, Type namedTypeSymbol)
        {
            Type = namedTypeSymbol;

            Name = Type.Name;
            FullName = Type.FullName;
            Namespace = Type.Namespace;

            IsUser = Type.GetCustomAttribute<IsUserAttribute>() != null;
            IsEntryPoint = Type.GetCustomAttribute<IsEntryPointAttribute>() != null;
            IsAuthorized = Type.GetCustomAttribute<IsAuthorizedAttribute>() != null;
            IsICrudEntity = typeof(ICrudEntity).IsAssignableFrom(Type);

            var isJunctionAttr = namedTypeSymbol.GetCustomAttribute<IsJunctionTableAttribute>();
            if (isJunctionAttr != null)
            {
                IsJunction = true;
                JunctionLeftRealType = new TypeHelper(isJunctionAttr.TypeLeft);
                JunctionRightRealType = new TypeHelper(isJunctionAttr.TypeRight);
            }

            Properties = Type
                .GetProperties()
                .Where(p =>
                    !string.IsNullOrWhiteSpace(p.Name) &&
                    !string.IsNullOrWhiteSpace(p.PropertyType.Namespace))
                .Select(propertyInfo => new DtoProperty(dataModel, this, propertyInfo))
                .ToArray();
        }

        public Type Type { get; }
        public string Name { get; }
        public string? FullName { get; }
        public string? Namespace { get; }
        public bool IsUser { get; }
        public bool IsEntryPoint { get; }
        public bool IsJunction { get; }
        public bool IsAuthorized { get; }
        public bool IsICrudEntity { get; }
        public TypeHelper? JunctionLeftRealType { get; }
        public TypeHelper? JunctionRightRealType { get; }
        public DtoProperty[] Properties { get; }
    }
}