using System;
using System.Collections.Generic;
using System.IO;

namespace gAPI.EntityFrameworkDisk.IndexSets;

public static class IndexSetCollection
{
    private static readonly Dictionary<Type, object> IndexSets =
        new Dictionary<Type, object>();
    public static IndexSet<T> GetOrCreate<T>(DirectoryInfo directory)
    {
        var entityType = typeof(T);
        if (IndexSets.TryGetValue(entityType, out var indexSet))
        {
            return (IndexSet<T>)indexSet;
        }
        else
        {
            var newEntityDefinition = new IndexSet<T>(directory);
            IndexSets[entityType] = newEntityDefinition;
            return newEntityDefinition;
        }
    }
}
