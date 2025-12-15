using gAPI.Attributes;
using gAPI.CodeGen.Frontend.Contexts;
using gAPI.CodeGen.Frontend.Helpers;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace gAPI.CodeGen.Frontend.Models.ServiceModels
{
    public class DtoProperty
    {
        public DtoProperty(ServiceContext dataModel, Dto dto, PropertyInfo propertyInfo)
        {
            DataModel = dataModel;
            Dto = dto;
            PropertyInfo = propertyInfo;

            Name = propertyInfo.Name;
            RealResponseType = propertyInfo.PropertyType;

            var context = new NullabilityInfoContext();
            var nullabilityInfo = context.Create(propertyInfo);
            IsNullable = nullabilityInfo.ReadState == NullabilityState.Nullable;

            IsForeignNameAttribute = propertyInfo.GetCustomAttribute<IsForeignNameAttribute>();
            IsStateManagedAttribute = propertyInfo.GetCustomAttribute<IsStateManagedAttribute>();
            IsNameAttribute = propertyInfo.GetCustomAttribute<IsNameAttribute>();
            IsReadOnly = propertyInfo.GetCustomAttribute<IsReadOnlyAttribute>() != null;
            IsUnique = propertyInfo.GetCustomAttribute<IsUniqueAttribute>() != null;
            IsKey = propertyInfo.GetCustomAttribute<KeyAttribute>() != null;
            IsStorageFile = propertyInfo.GetCustomAttribute<IsStorageFileAttribute>() != null;
        }

        public ServiceContext DataModel { get; }
        public Dto Dto { get; }
        public PropertyInfo PropertyInfo { get; }

        public string Name { get; }
        private Type RealResponseType { get; }
        public bool IsNullable { get; }

        public IsForeignNameAttribute? IsForeignNameAttribute { get; }
        public IsStateManagedAttribute? IsStateManagedAttribute { get; }
        public IsNameAttribute? IsNameAttribute { get; }

        public bool IsForeignName => IsForeignNameAttribute != null;
        public bool IsStateManaged => IsStateManagedAttribute != null;
        public bool IsName => IsNameAttribute != null;
        public bool IsReadOnly { get; }
        public bool IsUnique { get; }
        public bool IsKey { get; }
        public bool IsStorageFile { get; }

        TypeHelper? _PropertyType { get; set; }
        public TypeHelper PropertyType => _PropertyType = _PropertyType ?? new TypeHelper(RealResponseType, IsNullable);

        TypeDigger? _TypeDigger { get; set; }
        public TypeDigger TypeDigger => _TypeDigger = _TypeDigger ?? new TypeDigger(DataModel, RealResponseType, IsNullable);

    }
}