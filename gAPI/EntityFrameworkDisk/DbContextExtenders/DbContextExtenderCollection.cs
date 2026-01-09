using System;
using System.Collections.Generic;

namespace gAPI.EntityFrameworkDisk.DbContextExtenders;

public static class DbContextExtenderCollection
{
    private static readonly Dictionary<Type, DbContextExtender> DbContextExtenders =
        new Dictionary<Type, DbContextExtender>();

    public static DbContextExtender GetOrCreate(DbContext dbContext)
    {
        var type = dbContext.GetType();
        if (DbContextExtenders.TryGetValue(type, out var extender))
        {
            return extender;
        }
        else
        {
            var newExtender = DbContextExtenderFactory.CreateInstance(dbContext);
            DbContextExtenders[type] = newExtender;
            return newExtender;
        }
    }
}