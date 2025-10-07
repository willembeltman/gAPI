using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace gAPI.AutoMapper.Models
{
    public class EntityToDtoModel
    {
        public EntityToDtoModel(Type entityType, Type dtoType)
        {
            EntityType = entityType;
            DtoType = dtoType;

            EntityProperties = entityType
                .GetProperties()
                .Where(a => a.CanWrite && a.CanRead)
                .Select(propertyInfo => new EntityProperty(propertyInfo))
                .ToArray();

            DtoProperties = dtoType
                .GetProperties()
                .Where(a => a.CanWrite && a.CanRead)
                .Select(propertyInfo => new DtoProperty(propertyInfo, EntityProperties))
                .ToArray();

            var entityKey = EntityProperties.FirstOrDefault(p => p.IsKey != null);
            if (entityKey == null)
                throw new InvalidOperationException($"Entity type '{entityType.Name}' does not have a key property.");

            var dtoKey = DtoProperties.FirstOrDefault(p => p.Name == entityKey.Name);
            if (dtoKey == null)
                throw new InvalidOperationException($"DTO type '{dtoType.Name}' does not have a matching key property for entity key '{entityKey.Name}'.");

            DtoNameProperties = DtoProperties
                .Where(a => a.IsForeignName != null)
                .Select(a => new DtoNameProperty(a, a.IsForeignName, EntityProperties, DtoProperties, entityKey, dtoKey))
                .Where(a =>
                    a.EntityForeignKeyProperty != null &&
                    a.DtoForeignKeyProperty != null &&
                    a.ForeignEntityType != null &&
                    a.EntityForeignNavigationProperty != null &&
                    a.ForeignEntityNameProperties != null &&
                    a.ForeignEntityNameProperties.Length > 0)
                .ToArray();

            ForeignEntityNameProperties = DtoNameProperties
                .SelectMany(a => a.ForeignEntityNameProperties)
                .ToArray();

            MatchedDtoProperties = DtoProperties.Where(a => a.MatchedEntityProperty != null).ToArray();
            TempName = $"{EntityType.FullName.Replace(".", "")}{DtoType.FullName.Replace(".", "")}";
        }

        public Type EntityType { get; }
        public Type DtoType { get; }
        public EntityProperty[] EntityProperties { get; }
        public DtoProperty[] DtoProperties { get; }
        public DtoNameProperty[] DtoNameProperties { get; }
        public ForeignEntityNameProperty[] ForeignEntityNameProperties { get; }
        public DtoProperty[] MatchedDtoProperties { get; }
        public string TempName { get; }
    }
}
