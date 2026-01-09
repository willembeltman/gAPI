using System;

namespace gAPI.EntityFrameworkDisk.Navigator;

public class Navigator<T>
{
    public Navigator(
        Action<T, DbContext> extendDelegate,
        Func<T, DbContext, bool, bool> findForeignKeyUsageDelegate,
        string code)
    {
        ExtendDelegate = extendDelegate;
        FindForeignKeyUsageDelegate = findForeignKeyUsageDelegate;
        Code = code;
    }

    private readonly Action<T, DbContext> ExtendDelegate;
    private readonly Func<T, DbContext, bool, bool> FindForeignKeyUsageDelegate;
    public readonly string Code;

    /// <summary>
    /// Setups all navigation properties of the entity so they can be used.
    /// </summary>
    /// <param name="entity">The entity of which the navigation properties should be set.</param>
    /// <param name="dbContext">The DbContext needed to instanciate/get any sub-entity EntityFactories.</param>
    public void Extend(T entity, DbContext dbContext)
    {
        if (entity == null) throw new Exception("Entity cannot be null while extending");
        ExtendDelegate(entity, dbContext);
    }
    /// <summary>
    /// Finds any foreign key in the database point towards the supplied entity
    /// </summary>
    /// <param name="entity">The entity which we have to search references for.</param>
    /// <param name="dbContext">The DbContext needed to instanciate/get any sub-entity EntityFactories.</param>
    /// <param name="removeIfFound">Optional parameter to override set all foreign keys towards this entity to 0 or null.</param>
    /// <returns>There were any references found (and deleted/set to 0 or null)</returns>
    public bool FindForeignKeyUsage(T entity, DbContext dbContext, bool removeIfFound = false)
    {
        if (entity == null) throw new Exception("Entity cannot be null while extending");
        return FindForeignKeyUsageDelegate(entity, dbContext, removeIfFound);
    }
}