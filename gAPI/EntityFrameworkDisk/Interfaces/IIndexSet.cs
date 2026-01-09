using gAPI.EntityFrameworkDisk.IndexSets;
using System;
using System.Collections.Generic;

namespace gAPI.EntityFrameworkDisk.Interfaces;

public interface IIndexSet<T> : IIndexSet, IKeyedSet<T>
{
    void Add(T item, long dataPosition);
    void AddRange(IEnumerable<T> range, Func<T, long> AddDataCallback);
    IndexItem? Find(T item);
    IEnumerable<IndexItem> FindRange(IEnumerable<T> range);
    void Update(List<ChangedEntity<T>> changedEntities, Func<T, long> AddDataCallback);
}
public interface IIndexSet : IKeyedSet, IEnumerable<IndexItem>
{
    long Count { get; }
    void Clear();
    IndexItem? Find(object keyValue);
    IEnumerable<IndexItem> FindRange(IEnumerable<object> keys);
    bool Remove(IndexItem item);
    bool RemoveRange(IEnumerable<IndexItem> indexes);
}