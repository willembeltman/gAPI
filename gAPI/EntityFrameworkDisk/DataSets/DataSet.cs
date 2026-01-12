using gAPI.AutoComparer;
using gAPI.AutoSerialiser;
using gAPI.EntityFrameworkDisk.EntityDefinitions;
using gAPI.EntityFrameworkDisk.IndexSets;
using gAPI.EntityFrameworkDisk.Interfaces;
using gAPI.EntityFrameworkDisk.Locks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace gAPI.EntityFrameworkDisk.DataSets;

public class DataSet<T> : IDiskSet<T>
{
    private readonly string DataFullName;
    private readonly ReaderWriterLockSlim Lock;
    private readonly SerializerInstance<T> SerializerInstance;
    private readonly ComparerInstance<T, T> ComparerInstance;
    private readonly IndexSet<T> Indexes;
    private readonly EntityDefinition EntityDefinition;

    internal DataSet(DirectoryInfo? directory = null)
    {
        directory = directory ?? new DirectoryInfo(Environment.CurrentDirectory);
        DataFullName = Path.Combine(directory.FullName, typeof(T).Name + ".data");
        var info = new FileInfo(DataFullName);
        if (!info.Directory!.Exists)
            info.Directory.Create();
        Lock = LockCollection.GetOrCreate<DataSet<T>>();
        SerializerInstance = SerializerCollection.GetOrCreate<T>();
        ComparerInstance = AutoComparer.Comparer.GetInstance<T, T>();
        Indexes = IndexSetCollection.GetOrCreate<T>(directory);
        EntityDefinition = EntityDefinitionCollection.GetInstance<T>();
    }

    public long Count => Indexes.Count;
    public string? KeyName => EntityDefinition.KeyName;
    public Type? KeyType => EntityDefinition.KeyType;
    public object? GetKeyValue(T item) => EntityDefinition.GetKeyValue(item);

    public IEnumerable<T> GetEnumerable()
    {
        if (Indexes.Count == 0) yield break;

        try
        {
            Lock.EnterReadLock();

            using (var dataStream = File.OpenRead(DataFullName))
            using (var dataReader = new BinaryReader(dataStream))
                foreach (var item in Indexes)
                {
                    if (dataStream.Position != item.DataPosition)
                        dataStream.Position = item.DataPosition;
                    yield return SerializerInstance.Read(dataReader);
                }
        }
        finally
        {
            Lock.ExitReadLock();
        }
    }
    public IEnumerable<(T Item, IndexItem Index)> Find(object keyValue)
    {
        if (KeyType == null)
            throw new NotSupportedException("You cannot find items by key if there is not [Key] defined.");

        try
        {
            Lock.EnterReadLock();

            var index = Indexes.Find(keyValue);
            if (index != null)
            {
                using (var dataStream = File.OpenRead(DataFullName))
                using (var dataReader = new BinaryReader(dataStream))
                {
                    dataStream.Position = index.DataPosition;
                    var item = SerializerInstance.Read(dataReader);
                    yield return (item, index);
                }
            }
        }
        finally
        {
            Lock.ExitReadLock();
        }
    }
    public IEnumerable<(T Item, IndexItem Index)> FindRange(IEnumerable<object> keys)
    {
        if (KeyType == null)
            throw new NotSupportedException("You cannot find items by key if there is not [Key] defined.");

        try
        {
            Lock.EnterReadLock();

            var indexes = Indexes.FindRange(keys);
            using (var dataStream = File.OpenRead(DataFullName))
            using (var dataReader = new BinaryReader(dataStream))
                foreach (var index in indexes)
                {
                    if (index == null) continue;
                    dataStream.Position = index.DataPosition;
                    var item = SerializerInstance.Read(dataReader);
                    yield return (item, index);
                }
        }
        finally
        {
            Lock.ExitReadLock();
        }
    }
    public IEnumerator<T> GetEnumerator() => GetEnumerable().GetEnumerator();

    public void Update(List<ChangedEntity<T>> changedEntities)
    {
        try
        {
            Lock.EnterWriteLock();

            using (var datastream = File.Open(DataFullName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            using (var datawriter = new BinaryWriter(datastream))
            {
                datastream.Position = datastream.Length;

                Indexes.Update(changedEntities, (item) =>
                {
                    var position = datastream.Position;
                    SerializerInstance.Write(datawriter, item);
                    return position;
                });
            }
        }
        finally
        {
            Lock.ExitWriteLock();
        }
    }
    public void Add(T item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));

        try
        {
            Lock.EnterWriteLock();

            using (var datastream = File.Open(DataFullName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            using (var datawriter = new BinaryWriter(datastream))
            {
                datastream.Position = datastream.Length;

                Indexes.Add(item, datastream.Position);
                SerializerInstance.Write(datawriter, item);
            }
        }
        finally
        {
            Lock.ExitWriteLock();
        }
    }
    public void AddRange(IEnumerable<T> range)
    {
        if (range == null) throw new ArgumentNullException(nameof(range));

        try
        {
            Lock.EnterWriteLock();

            using (var datastream = File.Open(DataFullName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            using (var datawriter = new BinaryWriter(datastream))
            {
                datastream.Position = datastream.Length;

                Indexes.AddRange(range, (item) =>
                {
                    var position = datastream.Position;
                    SerializerInstance.Write(datawriter, item);
                    return position;
                });
            }
        }
        finally
        {
            Lock.ExitWriteLock();
        }
    }
    public bool Remove(T item)
    {
        try
        {
            Lock.EnterWriteLock();

            IndexItem? foundIndex = null;

            // Kunnen we hem opzoeken via de key?
            if (KeyType != null)
            {
                // Ja, dus opzoeken
                foundIndex = Indexes.Find(item);
            }
            else
            {
                // Nee, dus zelf scannen
                using (var dataStream = File.OpenRead(DataFullName))
                using (var dataReader = new BinaryReader(dataStream))
                {
                    foreach (var index in Indexes)
                    {
                        if (dataStream.Position != index.DataPosition)
                            dataStream.Position = index.DataPosition;
                        var dataItem = SerializerInstance.Read(dataReader);
                        if (!ComparerInstance.IsEqual(dataItem, item))
                        {
                            foundIndex = index;
                            break;
                        }
                    }
                }
            }

            if (foundIndex != null)
            {
                // En dan verwijderen indien gevonden
                Indexes.Remove(foundIndex);
            }

            return foundIndex != null;
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

            IndexItem[] foundIndexes;

            // Kunnen we hem opzoeken via de key?
            if (KeyType != null)
            {
                // Ja, dus opzoeken
                foundIndexes = Indexes
                    .FindRange(range)
                    .ToArray();
            }
            else
            {
                // Nee, dus zelf scannen

                // Eerst de range maar naar array omzetten aangezien het
                // mogelijk een 1 malige te consumen enumerator is.
                var rangeArray = range.ToArray();
                foundIndexes = new IndexItem[rangeArray.Length];

                // Dan de scan doen
                using (var dataStream = File.OpenRead(DataFullName))
                using (var dataReader = new BinaryReader(dataStream))
                {
                    foreach (var index in Indexes)
                    {
                        // Positie instellen
                        if (dataStream.Position != index.DataPosition)
                            dataStream.Position = index.DataPosition;

                        // Serialiseren
                        var item = SerializerInstance.Read(dataReader);

                        // En vergelijken met range
                        for (int i = 0; i < rangeArray.Length; i++)
                        {
                            if (!ComparerInstance.IsEqual(item, rangeArray[i]))
                            {
                                foundIndexes[i] = index;
                                break;
                            }
                        }

                        if (!foundIndexes.Any(a => a == null))
                            break;
                    }
                }
            }

            // En dan verwijderen indien gevonden
            return Indexes.RemoveRange(foundIndexes);
        }
        finally
        {
            Lock.ExitWriteLock();
        }
    }
    public bool RemoveRange(IEnumerable<object> keys)
    {
        if (KeyType == null)
            throw new NotSupportedException("You cannot find items by key if there is not [Key] defined.");

        try
        {
            Lock.EnterWriteLock();

            var foundIndexes = Indexes
                    .FindRange(keys)
                    .ToArray();

            // En dan verwijderen indien gevonden
            return Indexes.RemoveRange(foundIndexes);
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

            Indexes.Clear();
            File.Delete(DataFullName);
        }
        finally
        {
            Lock.ExitWriteLock();
        }
    }

    public bool Any(Expression<Func<T, bool>> expression)
    {
        var items = GetFindKeyFromExpression(expression);
        if (items != null) return items.Any();
        return GetEnumerable().AsQueryable().Any(expression);
    }
    public bool All(Expression<Func<T, bool>> expression)
    {
        var items = GetFindKeyFromExpression(expression);
        if (items != null) return items.AsQueryable().All(expression);
        return GetEnumerable().AsQueryable().All(expression);
    }
    public T Single(Expression<Func<T, bool>> expression)
    {
        var items = GetFindKeyFromExpression(expression);
        if (items != null) return items.Single();
        return GetEnumerable().AsQueryable().Single(expression);
    }
    public T? SingleOrDefault(Expression<Func<T, bool>> expression)
    {
        var items = GetFindKeyFromExpression(expression);
        if (items != null) return items.SingleOrDefault();
        return GetEnumerable().AsQueryable().SingleOrDefault(expression);
    }
    public T First(Expression<Func<T, bool>> expression)
    {
        var items = GetFindKeyFromExpression(expression);
        if (items != null) return items.First();
        return GetEnumerable().AsQueryable().First(expression);
    }
    public T? FirstOrDefault(Expression<Func<T, bool>> expression)
    {
        var items = GetFindKeyFromExpression(expression);
        if (items != null) return items.FirstOrDefault();
        return GetEnumerable().AsQueryable().FirstOrDefault(expression);
    }
    public T Last(Expression<Func<T, bool>> expression)
    {
        var items = GetFindKeyFromExpression(expression);
        if (items != null) return items.Last();
        return GetEnumerable().AsQueryable().Last(expression);
    }
    public T? LastOrDefault(Expression<Func<T, bool>> expression)
    {
        var items = GetFindKeyFromExpression(expression);
        if (items != null) return items.LastOrDefault();
        return GetEnumerable().AsQueryable().LastOrDefault(expression);
    }
    public IQueryable<T> Where(Expression<Func<T, bool>> expression)
    {
        var items = GetFindKeyFromExpression(expression);
        if (items != null) return items.AsQueryable();
        return GetEnumerable().AsQueryable().Where(expression);
    }

    private IEnumerable<T>? GetFindKeyFromExpression(Expression<Func<T, bool>> expression)
    {
        if (KeyName == null || KeyType == null) return null;

        if (expression.Body is BinaryExpression binary &&
            binary.NodeType == ExpressionType.Equal &&
            binary.Left is MemberExpression member &&
            binary.Right is ConstantExpression constant &&
            member.Member.Name == KeyName)
        {
            var keyValue = constant.Value;
            if (keyValue != null && keyValue.GetType() == KeyType)
            {
                return Find(keyValue).Select(a => a.Item);
            }
        }

        return null;
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
