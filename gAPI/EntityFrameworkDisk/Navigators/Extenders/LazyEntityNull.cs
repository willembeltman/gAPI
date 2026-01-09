using System;
using System.Linq;

namespace gAPI.EntityFrameworkDisk.Navigators.Extenders;


/// <summary>
/// Implements a lazy-loading foreign entity reference for entities implementing <see cref="IEntity"/>.
/// (with not nullable foreign keys)
/// </summary>
/// <typeparam name="TPrimary">The type of the primary entity that is being referenced.</typeparam>
/// <typeparam name="TForeign">The type of the foreign entity holding the foreign key.</typeparam>
public class LazyEntityNull<TPrimary, TForeign> : ILazy<TPrimary>
{
    private readonly DbSet<TPrimary> dbSet;
    private readonly TForeign foreign;
    private readonly Func<TPrimary, object> getPrimayKey;
    private readonly Func<TForeign, object?> getForeignKey;
    private readonly Action<TForeign, object?> setForeignKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="LazyEntityNotNull{TPrimary, TForeign}"/> class.
    /// </summary>
    /// <param name="dbSet">The DbSet to query for the primary entity.</param>
    /// <param name="foreign">The foreign entity instance containing the foreign key.</param>
    /// <param name="getForeignKey">Function to get the foreign key value from the foreign entity.</param>
    /// <param name="setForeignKey">Action to set the foreign key value on the foreign entity.</param>
    public LazyEntityNull(
        DbSet<TPrimary> dbSet,
        TForeign foreign,
        Func<TPrimary, object> getPrimayKey,
        Func<TForeign, object> getForeignKey,
        Action<TForeign, object?> setForeignKey)
    {
        this.dbSet = dbSet;
        this.foreign = foreign;
        this.getPrimayKey = getPrimayKey;
        this.getForeignKey = getForeignKey;
        this.setForeignKey = setForeignKey;
    }

    /// <summary>
    /// Gets or sets the lazily loaded primary entity.
    /// When getting, it loads the entity from the database if the cached entity is not up-to-date.
    /// When setting, it updates the cached entity and the foreign key on the foreign entity.
    /// If the new value is not yet persisted (Id &lt; 0), it is added to the <see cref="DbSet{T}"/>.
    /// </summary>
    public TPrimary? Value
    {
        get
        {
            var currentForeignKey = getForeignKey(foreign);
            if (currentForeignKey == null) return default;
            var item = dbSet.Find(currentForeignKey).FirstOrDefault();
            if (item == null) return default;
            return item;
        }
        set
        {
            var objValue = value == null ? null : getPrimayKey(value);
            setForeignKey(foreign, objValue);
        }
    }
}

///// <summary>
///// Implements a lazy-loading foreign entity reference for entities implementing <see cref="IEntity"/>.
///// (with nullsble foreign keys)
///// </summary>
///// <typeparam name="TPrimary">The type of the primary entity that is being referenced.</typeparam>
///// <typeparam name="TForeign">The type of the foreign entity holding the foreign key.</typeparam>
//public class LazyEntityNull<TPrimary, TForeign> : ILazy<TPrimary>
//    where TPrimary : class, IEntity
//{
//    private readonly DbSet<TPrimary> dbSet;
//    private readonly TForeign foreign;
//    private readonly Func<TForeign, long?> getForeignKey;
//    private readonly Action<TForeign, long?> setForeignKey;

//    /// <summary>
//    /// Initializes a new instance of the <see cref="LazyEntityNull{TPrimary, TForeign}"/> class.
//    /// </summary>
//    /// <param name="dbSet">The DbSet to query for the primary entity.</param>
//    /// <param name="foreign">The foreign entity instance containing the foreign key.</param>
//    /// <param name="getForeignKey">Function to get the foreign key value from the foreign entity.</param>
//    /// <param name="setForeignKey">Action to set the foreign key value on the foreign entity.</param>
//    public LazyEntityNull(
//        DbSet<TPrimary> dbSet,
//        TForeign foreign,
//        Func<TForeign, long?> getForeignKey,
//        Action<TForeign, long?> setForeignKey)
//    {
//        this.dbSet = dbSet;
//        this.foreign = foreign;
//        this.getForeignKey = getForeignKey;
//        this.setForeignKey = setForeignKey;
//    }

//    /// <summary>
//    /// Gets or sets the lazily loaded primary entity.
//    /// When getting, it loads the entity from the database if the cached entity is not up-to-date.
//    /// When setting, it updates the cached entity and the foreign key on the foreign entity.
//    /// If the new value is not yet persisted (Id &lt; 0), it is added to the <see cref="DbSet{T}"/>.
//    /// </summary>
//    public TPrimary Value
//    {
//        get
//        {
//            var currentForeignKey = getForeignKey(foreign);
//            if (currentForeignKey == null) return default;
//            var item = dbSet.Find(currentForeignKey.Value);
//            if (item == null) return default;
//            return item;
//        }
//        set
//        {
//            setForeignKey(foreign, value?.Id);
//        }
//    }
//}