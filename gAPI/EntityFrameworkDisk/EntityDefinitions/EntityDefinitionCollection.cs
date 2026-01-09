using System;
using System.Collections.Generic;

namespace gAPI.EntityFrameworkDisk.EntityDefinitions;

public static class EntityDefinitionCollection
{
    private static readonly Dictionary<Type, EntityDefinition> EntityDefinitions =
        new Dictionary<Type, EntityDefinition>();
    public static EntityDefinition GetInstance<T>()
    {
        var entityType = typeof(T);
        if (EntityDefinitions.TryGetValue(entityType, out var entityDefinition))
        {
            return entityDefinition;
        }
        else
        {
            var newEntityDefinition = new EntityDefinition(entityType);
            EntityDefinitions[entityType] = newEntityDefinition;
            return newEntityDefinition;
        }
    }
}
