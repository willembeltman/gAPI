using gAPI.Attributes;
using gAPI.AutoMapper.Helpers;
using Microsoft.CodeAnalysis;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace gAPI.AutoMapper.Models
{
    public class DtoProperty
    {
        public DtoProperty(
            PropertyInfo propertyInfo,
            EntityProperty[] entityProperties)
        {
            PropertyInfo = propertyInfo;
            Type = new TypeMapperInfo(PropertyType);

            IsKey = propertyInfo.GetCustomAttribute<KeyAttribute>();
            IsName = propertyInfo.GetCustomAttribute<IsNameAttribute>();
            IsForeignName = propertyInfo.GetCustomAttribute<IsForeignNameAttribute>();
            IsForeignKey = propertyInfo.GetCustomAttribute<IsForeignKeyAttribute>();
            IsReadOnly = propertyInfo.GetCustomAttribute<IsReadOnlyAttribute>();
            IsWriteOnly = propertyInfo.GetCustomAttribute<IsWriteOnlyAttribute>();

            MatchedEntityProperty = entityProperties.FirstOrDefault(p => p.Name == Name);
        }

        public PropertyInfo PropertyInfo { get; }
        public TypeMapperInfo Type { get; }
        public KeyAttribute IsKey { get; }
        public IsNameAttribute IsName { get; }
        public IsForeignNameAttribute IsForeignName { get; }
        public IsForeignKeyAttribute IsForeignKey { get; }
        public IsReadOnlyAttribute IsReadOnly { get; }
        public IsWriteOnlyAttribute IsWriteOnly { get; }
        public EntityProperty MatchedEntityProperty { get; }

        public string Name => PropertyInfo.Name;
        public Type PropertyType => PropertyInfo.PropertyType;
    }

}
