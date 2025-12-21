using gAPI.AutoClient.SignalR.Contexts;
using gAPI.AutoClient.SignalR.ServiceModels;
using Microsoft.CodeAnalysis;
using System.Linq;

namespace gAPI.AutoClient.SignalR.Helpers
{
    internal class TypeDigger
    {
        public TypeDigger(ServiceContext dataModel, ITypeSymbol typeSymbol, bool isNullable = false)
        {
            StartTypeSymbol = typeSymbol;
            StartType = new TypeHelper(dataModel, typeSymbol, isNullable);
            DataModel = dataModel;

            TypeSymbol = DigTillBase(typeSymbol);
            Type = new TypeHelper(dataModel, TypeSymbol, isNullable);

            Name = TypeSymbol.Name;
            FullName = TypeSymbol.ToDisplayString();
            Namespace = TypeSymbol.ContainingNamespace.ToDisplayString();
            IsValueType = TypeSymbol.IsValueType;
            IsNullable = isNullable;

            (Dto, Enum) = FindDtoOrEnum(dataModel, FullName);
        }

        private (Dto Dto, EnumDto Enum) FindDtoOrEnum(ServiceContext dataModel, string fullName)
        {
            Dto foundDto = null;
            EnumDto foundEnum = null;
            foreach (var dto in dataModel.Dtos)
            {
                if (dto.FullName == fullName)
                {
                    foundDto = dto;
                    break;
                }
            }
            if (Dto == null)
            {
                foreach (var enumDto in dataModel.Enums)
                {
                    if (enumDto.FullName == fullName)
                    {
                        foundEnum = enumDto;
                        break;
                    }
                }
            }
            return (foundDto, foundEnum);
        }

        private ITypeSymbol DigTillBase(ITypeSymbol type)
        {
            var resolved = type;
            while (true)
            {
                if (IsArrayType(resolved, out var arrayType))
                    resolved = GetUnderlayingTypeArray(arrayType);

                if (!IsGenericType(resolved))
                    break;

                var next = GetUnderlayingTypeGeneric(resolved);

                if (AreTypesEqual(next, resolved))
                    break;

                resolved = next;
            }
            return resolved;
        }
        private bool IsArrayType(ITypeSymbol resolved, out IArrayTypeSymbol arrayTypeSymbol)
        {
            arrayTypeSymbol = null;
            if (resolved is IArrayTypeSymbol arr)
            {
                arrayTypeSymbol = arr;
                return true;
            }
            return false;
        }
        private ITypeSymbol GetUnderlayingTypeArray(IArrayTypeSymbol arr)
        {
            return arr.ElementType;
        }
        private bool IsGenericType(ITypeSymbol type)
        {
            return type is INamedTypeSymbol namedType && namedType.IsGenericType;
        }
        private ITypeSymbol GetUnderlayingTypeGeneric(ITypeSymbol type)
        {
            // 1. Arrays → element type
            if (type is IArrayTypeSymbol arrayType)
            {
                return arrayType.ElementType;
            }

            // 2. Nullable<T> → T
            if (type.OriginalDefinition.ToDisplayString() == "System.Nullable<T>")
            {
                if (type is INamedTypeSymbol namedNullable && namedNullable.TypeArguments.Length == 1)
                    return namedNullable.TypeArguments[0];
            }

            // 3. IEnumerable<T>, List<T>, Task<T>, ActionResult<T>, etc.
            if (type is INamedTypeSymbol namedType && namedType.TypeArguments.Length == 1)
            {
                return namedType.TypeArguments[0];
            }

            // 4. Andere complexe types met meerdere type parameters
            // (Je zou hier kunnen kiezen voor een strategie — pak bijv. de laatste, of specifieke types herkennen)
            if (type is INamedTypeSymbol multiType && multiType.TypeArguments.Length > 1)
            {
                // Heuristisch: vaak zit payload in de laatste type argument
                return multiType.TypeArguments.Last();
            }

            // Geen onderliggend type gevonden, return hetzelfde
            return type;
        }
        private bool AreTypesEqual(ITypeSymbol next, ITypeSymbol resolved)
        {
            return SymbolEqualityComparer.Default.Equals(next, resolved);
        }


        public Dto Dto { get; }
        public EnumDto Enum { get; }
        public ITypeSymbol StartTypeSymbol { get; }
        public TypeHelper StartType { get; }
        public ServiceContext DataModel { get; }
        public ITypeSymbol TypeSymbol { get; }
        public TypeHelper Type { get; }
        public string Name { get; }
        public string FullName { get; }
        public string Namespace { get; }
        public bool IsValueType { get; }
        public bool IsNullable { get; }
    }
}