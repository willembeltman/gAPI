using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace gAPI.EntityFrameworkDisk.Interfaces;

public interface ILongCollection<T> : IEnumerable<T>
{
    void Add(T item);
    void AddRange(IEnumerable<T> range);
    bool All(Expression<Func<T, bool>> expression);
    bool Any(Expression<Func<T, bool>> expression);
    T First(Expression<Func<T, bool>> expression);
    T? FirstOrDefault(Expression<Func<T, bool>> expression);
    T Last(Expression<Func<T, bool>> expression);
    T? LastOrDefault(Expression<Func<T, bool>> expression);
    bool Remove(T item);
    bool RemoveRange(IEnumerable<T> range);
    T Single(Expression<Func<T, bool>> expression);
    T? SingleOrDefault(Expression<Func<T, bool>> expression);
    IQueryable<T> Where(Expression<Func<T, bool>> expression);
}

public interface ILongCollection
{
    void Clear();
    long Count { get; }
}