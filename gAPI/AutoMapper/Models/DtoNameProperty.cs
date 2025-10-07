using gAPI.Attributes;
using gAPI.Interfaces;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace gAPI.AutoMapper.Models
{
    public class DtoNameProperty : INameProperty
    {
        public DtoNameProperty(
            DtoProperty dtoProperty,
            IsForeignNameAttribute isForeignName,
            EntityProperty[] entityProperties,
            DtoProperty[] dtoProperties,
            EntityProperty entityKey,
            DtoProperty dtoKey)
        {
            DtoProperty = dtoProperty;

            DtoForeignKeyProperty = dtoProperties
                .FirstOrDefault(a => a.Name == isForeignName.ForeignKeyName);
            if (DtoForeignKeyProperty == null)
                return;

            EntityForeignKeyProperty = DtoForeignKeyProperty.MatchedEntityProperty;
            if (EntityForeignKeyProperty == null)
                return;

            EntityForeignNavigationProperty = entityProperties
                .FirstOrDefault(a =>
                    a.ForeignKey != null && a.ForeignKey.Name == EntityForeignKeyProperty.Name);
            if (EntityForeignNavigationProperty == null)
            {
                var foreignNavigationName = EntityForeignKeyProperty.Name;
                if (foreignNavigationName.EndsWith(entityKey.Name, StringComparison.OrdinalIgnoreCase))
                    foreignNavigationName = foreignNavigationName.Substring(0, foreignNavigationName.Length - entityKey.Name.Length);
                EntityForeignNavigationProperty = entityProperties
                    .FirstOrDefault(a => a.Name == foreignNavigationName);
            }

            if (EntityForeignNavigationProperty == null)
                return;

            ForeignEntityType = EntityForeignNavigationProperty.PropertyType;

            ForeignEntityNameProperties = ForeignEntityType
                .GetProperties()
                .Select(a => new ForeignEntityNameProperty(this, a))
                .Where(a => a.IsName != null)
                .ToArray();
        }

        public DtoProperty DtoProperty { get; }
        public DtoProperty? DtoForeignKeyProperty { get; }
        public EntityProperty? EntityForeignKeyProperty { get; }
        public EntityProperty? EntityForeignNavigationProperty { get; }
        public Type? ForeignEntityType { get; }
        public ForeignEntityNameProperty[]? ForeignEntityNameProperties { get; }
        public string Name => DtoProperty.Name;
    }

}
