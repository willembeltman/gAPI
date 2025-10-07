using gAPI.AutoComparer;
using gAPI.EntityFrameworkDisk.DataSets;
using gAPI.EntityFrameworkDisk.EntityDefinitions;
using gAPI.EntityFrameworkDisk.Interfaces;
using gAPI.EntityFrameworkDisk.Navigator;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace gAPI.EntityFrameworkDisk
{
    public class DbSet<T> : IDbSet<T>
    {
        private readonly DbContext DbContext;
        private readonly Queue<T> AddCache;
        private readonly Dictionary<object, T> ChangeCache;
        private readonly Dictionary<object, T> RemoveCache;
        private readonly ReaderWriterLockSlim Lock;
        private readonly DataSet<T> DataSet;
        private readonly ComparerInstance<T, T> ComparerInstance;
        private readonly Navigator<T> Navigator;
        private readonly EntityDefinition EntityDefinition;

        // Wordt aangeroepen vauit de gegeneerde code
        public DbSet(DbContext dbContext, DirectoryInfo directory)
        {
            dbContext.AddDbSet(this);
            DbContext = dbContext;

            AddCache = new Queue<T>();
            ChangeCache = new Dictionary<object, T>();
            RemoveCache = new Dictionary<object, T>();
            Lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
            DataSet = DataSetCollection.GetInstance<T>(directory);
            ComparerInstance = AutoComparer.Comparer.GetInstance<T, T>();
            Navigator = NavigatorCollection.GetInstance<T>(dbContext);
            EntityDefinition = EntityDefinitionCollection.GetInstance<T>();

            if (KeyName == null || KeyType == null)
                throw new NotSupportedException("The DbSet requires a [Key] property on all entities.");
        }

        public long Count => DataSet.Count + AddCache.Count - RemoveCache.Count;
        public string? KeyName => EntityDefinition.KeyName;
        public Type? KeyType => EntityDefinition.KeyType;
        public object? GetKeyValue(T item) => EntityDefinition.GetKeyValue(item);

        public IEnumerator<T> GetEnumerator()
        {
            try
            {
                Lock.EnterReadLock();

                if (ChangeCache.Count == DataSet.Count)
                {
                    foreach (var item in ChangeCache.Values)
                        yield return item;
                }
                else
                {
                    var list = DataSet.GetEnumerable();
                    foreach (var item in list)
                    {
                        var key = EntityDefinition.GetKeyValue(item);
                        if (key == null)
                            throw new NotSupportedException("The DbSet requires a [Key] property on all entities.");

                        if (RemoveCache.ContainsKey(key))
                            continue;

                        var res = AddToCache(item, key);
                        if (res == null)
                            continue;

                        yield return res;
                    }
                }

                foreach (var item in AddCache)
                    yield return item;
            }
            finally
            {
                Lock.ExitReadLock();
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Attach(T item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            var key = GetKeyValue(item);
            if (key == null)
                throw new NotSupportedException("Please supply a [Key] to each entity.");
            var dbItem = Find(key);

            if (!dbItem.Any())
            {
                Add(item);
                return;
            }
            else
            {
                try
                {
                    Lock.EnterWriteLock();

                    ChangeCache[key] = item;
                    Navigator.Extend(item, DbContext);
                }
                finally
                {
                    Lock.ExitWriteLock();
                }
            }
        }
        public IEnumerable<T> Find(object keyValue)
        {
            if (KeyType == null)
                throw new NotSupportedException("You cannot find items by key if there is not [Key] defined.");

            if (ChangeCache.TryGetValue(keyValue, out var cacheItem))
            {
                yield return cacheItem;
                yield break;
            }

            try
            {
                Lock.EnterReadLock();

                var indexItems = DataSet.Find(keyValue);
                foreach (var indexItem in indexItems)
                {
                    var item = indexItem.Item;
                    Navigator.Extend(item, DbContext);
                    yield return item;
                }
            }
            finally
            {
                Lock.ExitReadLock();
            }
        }
        public IEnumerable<T> FindRange(IEnumerable<object> keys)
        {
            if (KeyType == null)
                throw new NotSupportedException("You cannot find items by key if there is not [Key] defined.");

            try
            {
                Lock.EnterReadLock();

                var indexItems = DataSet.FindRange(keys);
                foreach (var indexItem in indexItems)
                {
                    var item = indexItem.Item;
                    Navigator.Extend(item, DbContext);
                    yield return item;
                }
            }
            finally
            {
                Lock.ExitReadLock();
            }
        }
        public void Add(T item)
        {
            try
            {
                Lock.EnterWriteLock();

                AddCache.Enqueue(item);
            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }
        public void AddRange(IEnumerable<T> range)
        {
            try
            {
                Lock.EnterWriteLock();

                foreach (var item in range)
                    AddCache.Enqueue(item);
            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }
        public bool Remove(T item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            try
            {
                Lock.EnterWriteLock();

                var key = EntityDefinition.GetKeyValue(item);
                if (key == null)
                    throw new NotSupportedException("The DbSet requires a [Key] property on all entities.");
                if (key.ToString() == "0")
                    throw new NotSupportedException("You cannot remove a new item.");

                if (!RemoveCache.ContainsKey(key))
                    RemoveCache.Add(key, item);

                return true;
            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }
        public bool RemoveRange(IEnumerable<T> range)
        {
            try
            {
                Lock.EnterWriteLock();

                foreach (var item in range)
                {
                    var key = EntityDefinition.GetKeyValue(item);
                    if (key == null)
                        throw new NotSupportedException("The DbSet requires a [Key] property on all entities.");
                    if (key.ToString() == "0")
                        throw new NotSupportedException("You cannot remove a new item.");
                    RemoveCache.Add(key, item);
                }
                return true;
            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }
        public void Clear()
        {
            try
            {
                Lock.EnterWriteLock();

                AddCache.Clear();
                ChangeCache.Clear();
                RemoveCache.Clear();

                DataSet.Clear();
            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }

        public bool Any(Expression<Func<T, bool>> expression)
            => DataSet.Any(expression);
        public bool All(Expression<Func<T, bool>> expression)
            => DataSet.All(expression);
        public T Single(Expression<Func<T, bool>> expression)
            => AddToCache(DataSet.Single(expression))!;
        public T? SingleOrDefault(Expression<Func<T, bool>> expression)
            => AddToCache(DataSet.SingleOrDefault(expression));
        public T First(Expression<Func<T, bool>> expression)
            => AddToCache(DataSet.First(expression))!;
        public T? FirstOrDefault(Expression<Func<T, bool>> expression)
            => AddToCache(DataSet.FirstOrDefault(expression));
        public T Last(Expression<Func<T, bool>> expression)
            => AddToCache(DataSet.Last(expression))!;
        public T? LastOrDefault(Expression<Func<T, bool>> expression)
            => AddToCache(DataSet.LastOrDefault(expression));
        public IQueryable<T> Where(Expression<Func<T, bool>> expression)
            => DataSet
                .Where(expression)
                .Select(a => AddToCache(a, null)!)
                .AsQueryable();
        private T? AddToCache(T item, object? key = null)
        {
            if (item == null) return default;

            key = key ?? EntityDefinition.GetKeyValue(item);
            if (key == null)
                throw new NotSupportedException("The DbSet requires a [Key] property on all entities.");

            if (!ChangeCache.ContainsKey(key))
            {
                ChangeCache.Add(key, item);
            }

            Navigator.Extend(item, DbContext);

            return item;
        }

        public void SaveChanges()
        {
            try
            {
                Lock.EnterWriteLock();

                // Remove check
                foreach (var item in RemoveCache.Values)
                    if (Navigator.FindForeignKeyUsage(item, DbContext))
                        throw new Exception("Foreign key found towards the item to remove");

                // Changed
                var changedEntityList = new List<ChangedEntity<T>>();
                foreach (var item in DataSet)
                {
                    var itemKey = EntityDefinition.GetKeyValue(item)
                        ?? throw new NotSupportedException("The DbSet requires a [Key] property on all entities.");

                    if (ChangeCache.ContainsKey(itemKey))
                    {
                        var cacheItem = ChangeCache[itemKey];
                        ChangeCache.Remove(itemKey);
                        if (!ComparerInstance.IsEqual(cacheItem, item))
                        {
                            changedEntityList.Add(new ChangedEntity<T>(itemKey, cacheItem));
                        }
                    }
                }
                if (changedEntityList.Count != 0)
                    DataSet.Update(changedEntityList);

                // Add
                DataSet.AddRange(AddCache);

                while (AddCache.Count > 0)
                {
                    var item = AddCache.Dequeue();
                    Navigator.Extend(item, DbContext);
                }

                // Remove
                DataSet.RemoveRange(RemoveCache.Keys);
                foreach (var item in RemoveCache.Keys)
                    ChangeCache.Remove(item);
                RemoveCache.Clear();
            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }

        public IEnumerable<object> GetAddedEntities()
        {
            try
            {
                Lock.EnterReadLock();

                foreach (var item in AddCache)
                    yield return item!;
            }
            finally
            {
                Lock.ExitReadLock();
            }
        }
        public IEnumerable<ChangedEntityObject> GetChangedEntities()
        {
            try
            {
                Lock.EnterReadLock();

                foreach (var item in DataSet)
                {
                    var itemKey = EntityDefinition.GetKeyValue(item)
                        ?? throw new NotSupportedException("The DbSet requires a [Key] property on all entities.");

                    if (ChangeCache.ContainsKey(itemKey))
                    {
                        var cacheItem = ChangeCache[itemKey];
                        if (!ComparerInstance.IsEqual(cacheItem, item) && cacheItem != null)
                        {
                            yield return new ChangedEntityObject(itemKey, cacheItem);
                        }
                    }
                }
            }
            finally
            {
                Lock.ExitReadLock();
            }
        }
        public IEnumerable<object> GetRemoveEntities()
        {
            try
            {
                Lock.EnterReadLock();

                foreach (var item in RemoveCache)
                    yield return item;
            }
            finally
            {
                Lock.ExitReadLock();
            }
        }
    }
}