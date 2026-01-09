using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace gAPI.EntityFrameworkDisk.Navigators.Extenders;

/// <summary>
/// Represents a lazy-loading collection of foreign entities related to a primary entity. 
/// (with not nullable foreign keys)
/// </summary>
/// <typeparam name="TForeign">The type of the foreign entity.</typeparam>
/// <typeparam name="TPrimary">The type of the primary entity.</typeparam>
public class LazyEntityCollectionNull<TForeign, TPrimary> : ICollection<TForeign>
{
    private readonly DbSet<TForeign> dbSet;
    private readonly TPrimary primary;
    private readonly Func<TPrimary, object> getPrimaryKey;
    private readonly Func<TForeign, object?> getForeign_PrimaryKey;
    private readonly Func<TForeign, object?> getForeignKey;
    private readonly Action<TForeign, object?> setForeignKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="LazyEntityCollectionNotNull{TForeign, TPrimary}"/> class.
    /// </summary>
    /// <param name="dbSet">The DbSet containing the foreign entities.</param>
    /// <param name="primary">The primary entity to which the foreign entities belong.</param>
    /// <param name="getForeignKey">Function to get the foreign key value from a foreign entity.</param>
    /// <param name="setForeignKey">Action to set the foreign key value on a foreign entity.</param>
    public LazyEntityCollectionNull(
        DbSet<TForeign> dbSet,
        TPrimary primary,
        Func<TPrimary, object> getPrimaryKey,
        Func<TForeign, object?> getForeign_PrimaryKey,
        Func<TForeign, object?> getForeignKey,
        Action<TForeign, object?> setForeignKey)
    {
        this.dbSet = dbSet;
        this.primary = primary;
        this.getPrimaryKey = getPrimaryKey;
        this.getForeign_PrimaryKey = getForeign_PrimaryKey;
        this.getForeignKey = getForeignKey;
        this.setForeignKey = setForeignKey;
    }

    /// <summary>
    /// Gets the number of foreign entities related to the primary entity.
    /// </summary>
    public int Count
    {
        get
        {
            var primaryKey = getPrimaryKey(primary);
            return dbSet.Count(a => getForeignKey(a) == primaryKey);
        }
    }

    /// <summary>
    /// Gets a value indicating whether the collection is read-only.
    /// Always <c>false</c>.
    /// </summary>
    public bool IsReadOnly => false;

    /// <summary>
    /// Adds a foreign entity to the collection and sets its foreign key to the primary entity's ID.
    /// </summary>
    /// <param name="item">The foreign entity to add.</param>
    public void Add(TForeign item)
    {
        setForeignKey(item, getPrimaryKey(primary));
        dbSet.Add(item);
    }

    /// <summary>
    /// Copies the foreign entities related to the primary entity to an array starting at the specified index.
    /// </summary>
    /// <param name="array">The destination array.</param>
    /// <param name="arrayIndex">The zero-based index in the array at which copying begins.</param>
    /// <exception cref="ArgumentException">Thrown if the destination array is too small.</exception>
    public void CopyTo(TForeign?[] array, int arrayIndex)
    {
        var primaryKey = getPrimaryKey(primary);
        foreach (var item in dbSet.Where(a => primaryKey.Equals(getForeignKey(a))))
        {
            if (arrayIndex >= array.Length) throw new ArgumentException("Target array too small");
            array[arrayIndex++] = item;
        }
    }
    /// <summary>
    /// Removes the specified foreign entity from the collection if it belongs to the primary entity.
    /// </summary>
    /// <param name="item">The foreign entity to remove.</param>
    /// <returns><c>true</c> if the entity was removed; otherwise, <c>false</c>.</returns>
    public bool Remove(TForeign item)
    {
        var primaryKey = getPrimaryKey(primary);
        var foreignPrimaryKey = getForeign_PrimaryKey(item);
        if (foreignPrimaryKey == null)
            return false;
        if (!dbSet.Any(foreign =>
            primaryKey.Equals(getForeignKey(foreign)) &&
            foreignPrimaryKey.Equals(getForeign_PrimaryKey(foreign)) == true))
            return false;
        setForeignKey(item, null);
        return true;
    }

    /// <summary>
    /// Removes all foreign entities related to the primary entity from the collection.
    /// </summary>
    public void Clear()
    {
        var primaryKey = getPrimaryKey(primary);
        var list = dbSet.Where(a => primaryKey.Equals(getForeignKey(a))).ToArray();
        dbSet.RemoveRange(list);
    }

    /// <summary>
    /// Determines whether the specified foreign entity belongs to the primary entity.
    /// </summary>
    /// <param name="item">The foreign entity to locate.</param>
    /// <returns><c>true</c> if the entity is found; otherwise, <c>false</c>.</returns>
    public bool Contains(TForeign item)
    {
        var primaryKey = getPrimaryKey(primary);
        var foreignPrimaryKey = getForeign_PrimaryKey(item);
        if (foreignPrimaryKey == null) return false;
        return dbSet.Any(foreign =>
            primaryKey.Equals(getForeignKey(foreign)) &&
            foreignPrimaryKey.Equals(getForeign_PrimaryKey(foreign)));
    }

    /// <summary>
    /// Returns an enumerator that iterates through the foreign entities related to the primary entity.
    /// </summary>
    /// <returns>An enumerator for the collection.</returns>
    public IEnumerator<TForeign> GetEnumerator()
    {
        var primaryKey = getPrimaryKey(primary);
        foreach (var item in dbSet.Where(a => primaryKey.Equals(getForeignKey(a))))
        {
            yield return item;
        }
    }

    /// <summary>
    /// Returns an enumerator that iterates through the foreign entities related to the primary entity.
    /// </summary>
    /// <returns>An enumerator for the collection.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

///// <summary>
///// Represents a lazy-loading collection of foreign entities related to a primary entity.
///// (with nullable foreign keys)
///// </summary>
///// <typeparam name="TForeign">The type of the foreign entity.</typeparam>
///// <typeparam name="TPrimary">The type of the primary entity.</typeparam>
//public class LazyEntityCollectionNull<TForeign, TPrimary> : ICollection<TForeign>
//    where TForeign : class, IEntity
//    where TPrimary : IEntity
//{
//    private readonly DbSet<TForeign> dbSet;
//    private readonly TPrimary primary;
//    private readonly Func<TForeign, long?> getForeignKey;
//    private readonly Action<TForeign, long?> setForeignKey;

//    /// <summary>
//    /// Initializes a new instance of the <see cref="LazyEntityCollectionNotNull{TForeign, TPrimary}"/> class.
//    /// </summary>
//    /// <param name="dbSet">The DbSet containing the foreign entities.</param>
//    /// <param name="primary">The primary entity to which the foreign entities belong.</param>
//    /// <param name="getForeignKey">Function to get the foreign key value from a foreign entity.</param>
//    /// <param name="setForeignKey">Action to set the foreign key value on a foreign entity.</param>
//    public LazyEntityCollectionNull(
//        DbSet<TForeign> dbSet,
//        TPrimary primary,
//        Func<TForeign, long?> getForeignKey,
//        Action<TForeign, long?> setForeignKey)
//    {
//        this.dbSet = dbSet;
//        this.primary = primary;
//        this.getForeignKey = getForeignKey;
//        this.setForeignKey = setForeignKey;
//    }

//    /// <summary>
//    /// Gets the number of foreign entities related to the primary entity.
//    /// </summary>
//    public int Count => dbSet.Count(a => getForeignKey(a) == primary.Id);
//    /// <summary>
//    /// Gets a value indicating whether the collection is read-only.
//    /// Always <c>false</c>.
//    /// </summary>
//    public bool IsReadOnly => false;

//    /// <summary>
//    /// Adds a foreign entity to the collection and sets its foreign key to the primary entity's ID.
//    /// </summary>
//    /// <param name="item">The foreign entity to add.</param>
//    public void Add(TForeign item)
//    {
//        setForeignKey(item, primary.Id);
//        dbSet.Add(item);
//    }

//    /// <summary>
//    /// Copies the foreign entities related to the primary entity to an array starting at the specified index.
//    /// </summary>
//    /// <param name="array">The destination array.</param>
//    /// <param name="arrayIndex">The zero-based index in the array at which copying begins.</param>
//    /// <exception cref="ArgumentException">Thrown if the destination array is too small.</exception>
//    public void CopyTo(TForeign[] array, int arrayIndex)
//    {
//        foreach (var item in dbSet.Where(a => getForeignKey(a) == primary.Id))
//        {
//            if (arrayIndex >= array.Length) throw new ArgumentException("Target array too small");
//            array[arrayIndex++] = item;
//        }
//    }
//    /// <summary>
//    /// Removes the specified foreign entity from the collection if it belongs to the primary entity.
//    /// </summary>
//    /// <param name="item">The foreign entity to remove.</param>
//    /// <returns><c>true</c> if the entity was removed; otherwise, <c>false</c>.</returns>
//    public bool Remove(TForeign item)
//    {
//        if (!dbSet.Any(a => getForeignKey(a) == primary.Id && a.Id == item.Id)) return false;
//        setForeignKey(item, 0);
//        return true;
//        //return dbSet.Remove(item);
//    }

//    /// <summary>
//    /// Removes all foreign entities related to the primary entity from the collection.
//    /// </summary>
//    public void Clear()
//    {
//        var list = dbSet.Where(a => getForeignKey(a) == primary.Id).ToArray();
//        dbSet.RemoveRange(list);
//    }

//    /// <summary>
//    /// Determines whether the specified foreign entity belongs to the primary entity.
//    /// </summary>
//    /// <param name="item">The foreign entity to locate.</param>
//    /// <returns><c>true</c> if the entity is found; otherwise, <c>false</c>.</returns>
//    public bool Contains(TForeign item)
//    {
//        return dbSet.Any(a => getForeignKey(a) == primary.Id && a.Id == item.Id);
//    }

//    /// <summary>
//    /// Returns an enumerator that iterates through the foreign entities related to the primary entity.
//    /// </summary>
//    /// <returns>An enumerator for the collection.</returns>
//    public IEnumerator<TForeign> GetEnumerator()
//    {
//        foreach (var item in dbSet.Where(a => getForeignKey(a) == primary.Id))
//        {
//            yield return item;
//        }
//    }

//    /// <summary>
//    /// Returns an enumerator that iterates through the foreign entities related to the primary entity.
//    /// </summary>
//    /// <returns>An enumerator for the collection.</returns>
//    IEnumerator IEnumerable.GetEnumerator()
//    {
//        return GetEnumerator();
//    }
//}