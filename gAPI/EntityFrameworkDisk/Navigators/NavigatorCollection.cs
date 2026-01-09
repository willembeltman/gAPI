using System;
using System.Collections.Generic;

namespace gAPI.EntityFrameworkDisk.Navigator;

public static class NavigatorCollection
{
    private static readonly Dictionary<Type, object> Navigators = new Dictionary<Type, object>();

    public static Navigator<T> GetInstance<T>(DbContext dbContext)
    {
        var entityType = typeof(T);
        if (Navigators.TryGetValue(entityType, out var serializer))
        {
            return (Navigator<T>)serializer;
        }
        else
        {
            var newNavigator = NavigatorFactory<T>.CreateInstance(dbContext);
            Navigators[entityType] = newNavigator;
            return newNavigator;
        }
    }
}