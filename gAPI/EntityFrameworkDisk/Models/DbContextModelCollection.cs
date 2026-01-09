using System;
using System.Collections.Generic;

namespace gAPI.EntityFrameworkDisk.Models;

public static class DbContextModelCollection
{
    private static readonly Dictionary<Type, DbContextModel> DbContextModels =
        new Dictionary<Type, DbContextModel>();
    public static DbContextModel GetOrCreate(Type dbContextType)
    {
        if (DbContextModels.TryGetValue(dbContextType, out var dbContextModel))
        {
            return dbContextModel;
        }
        else
        {
            var newEntityDefinition = new DbContextModel(dbContextType);
            DbContextModels[dbContextType] = newEntityDefinition;
            return newEntityDefinition;
        }
    }
}