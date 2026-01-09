using System;
using System.Collections.Generic;
using System.IO;

namespace gAPI.EntityFrameworkDisk.DataSets;

public static class DataSetCollection
{
    private static readonly Dictionary<Type, object> DiskSets =
        new Dictionary<Type, object>();
    public static DataSet<T> GetInstance<T>(DirectoryInfo? directory = null)
    {
        var entityType = typeof(T);
        if (DiskSets.TryGetValue(entityType, out var diskSet))
        {
            return (DataSet<T>)diskSet;
        }
        else
        {
            var newEntityDefinition = new DataSet<T>(directory);
            DiskSets[entityType] = newEntityDefinition;
            return newEntityDefinition;
        }
    }
}
