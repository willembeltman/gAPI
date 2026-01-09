using System;
using System.Collections.Generic;

namespace gAPI.EntityFrameworkDisk;

public static class DbContextCollection<T>
    where T : class, new()
{
    private static readonly Dictionary<Type, T> DbContextExtenders =
        new Dictionary<Type, T>();

    public static T GetOrCreate()
    {
        var type = typeof(T);
        if (DbContextExtenders.TryGetValue(type, out var extender))
        {
            return extender;
        }
        else
        {
            var newDbContext = new T();
            DbContextExtenders[type] = newDbContext;
            return newDbContext;
        }
    }
}