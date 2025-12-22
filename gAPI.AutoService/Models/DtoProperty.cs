using gAPI.AutoService.Helpers;
using Microsoft.CodeAnalysis;
using System.Linq;

namespace gAPI.AutoService.Models
{
    internal class DtoProperty
    {
        internal DtoProperty(ServiceContext dataModel, Dto dto, IPropertySymbol propertySymbol)
        {
            DataModel = dataModel;
            Dto = dto;
            PropertySymbol = propertySymbol;

            Name = propertySymbol.Name;
            ResponseTypeSymbol = propertySymbol.Type;

            IsNullable = propertySymbol.NullableAnnotation == NullableAnnotation.Annotated;
            IsReadOnly = propertySymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "IsReadOnlyAttribute");
            IsForeignName = propertySymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "IsForeignNameAttribute");
            IsStateManaged = propertySymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "IsStateManagedAttribute");
            IsUnique = propertySymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "IsUniqueAttribute");
            IsKey = propertySymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "KeyAttribute");
            IsStorageFile = propertySymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "IsStorageFileAttribute");
        }

        public ServiceContext DataModel { get; }
        public Dto Dto { get; }
        public IPropertySymbol PropertySymbol { get; }

        public string Name { get; }
        public ITypeSymbol ResponseTypeSymbol { get; }
        public bool IsNullable { get; }
        public bool IsReadOnly { get; }
        public bool IsForeignName { get; }
        public bool IsStateManaged { get; }
        public bool IsUnique { get; }
        public bool IsKey { get; }
        public bool IsStorageFile { get; }
        TypeHelper _PropertyType { get; set; }
        public TypeHelper PropertyType => _PropertyType = _PropertyType ?? new TypeHelper(DataModel, ResponseTypeSymbol, IsNullable);

        TypeDigger _TypeRapport { get; set; }
        public TypeDigger TypeRapport => _TypeRapport = _TypeRapport ?? new TypeDigger(DataModel, ResponseTypeSymbol);
    }
}