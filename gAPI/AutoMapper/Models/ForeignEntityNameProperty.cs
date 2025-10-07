using gAPI.Attributes;
using gAPI.AutoMapper.Helpers;
using gAPI.Interfaces;
using System;
using System.Reflection;

namespace gAPI.AutoMapper.Models
{
    public class ForeignEntityNameProperty : INameProperty
    {
        public ForeignEntityNameProperty(DtoNameProperty dtoNameProperty, PropertyInfo propertyInfo)
        {
            DtoNameProperty = dtoNameProperty;
            PropertyInfo = propertyInfo;
            Type = new TypeMapperInfo(PropertyType);

            IsName = propertyInfo.GetCustomAttribute<IsNameAttribute>();
        }

        public DtoNameProperty DtoNameProperty { get; }
        public PropertyInfo PropertyInfo { get; }
        public TypeMapperInfo Type { get; }
        public IsNameAttribute IsName { get; }

        public string Name => PropertyInfo.Name;
        public string ExternalName => $"{DtoNameProperty.EntityForeignNavigationProperty!.Name}.{PropertyInfo.Name}";
        public string ExternalNameSafe => $"{DtoNameProperty.EntityForeignNavigationProperty!.Name}?.{PropertyInfo.Name} ?? default";
        public string TempName => $"{DtoNameProperty.Name}_{Name}";
        public Type PropertyType => PropertyInfo.PropertyType;

    }

}
