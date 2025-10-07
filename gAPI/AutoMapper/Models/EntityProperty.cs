using gAPI.Attributes;
using gAPI.Interfaces;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace gAPI.AutoMapper.Models
{
    public class EntityProperty : INameProperty
    {
        public EntityProperty(PropertyInfo propertyInfo)
        {
            PropertyInfo = propertyInfo;

            IsKey = propertyInfo.GetCustomAttribute<KeyAttribute>();
            IsName = propertyInfo.GetCustomAttribute<IsNameAttribute>();
            ForeignKey = propertyInfo.GetCustomAttribute<ForeignKeyAttribute>();
        }

        public PropertyInfo PropertyInfo { get; }
        public KeyAttribute IsKey { get; }
        public IsNameAttribute IsName { get; }
        public ForeignKeyAttribute ForeignKey { get; }

        public string Name => PropertyInfo.Name;
        public Type PropertyType => PropertyInfo.PropertyType;
    }

}
