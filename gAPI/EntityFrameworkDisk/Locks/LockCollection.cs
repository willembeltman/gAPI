using System;
using System.Collections.Generic;
using System.Threading;

namespace gAPI.EntityFrameworkDisk.Locks;

internal static class LockCollection
{
    private static readonly Dictionary<Type, ReaderWriterLockSlim> Locks =
        new Dictionary<Type, ReaderWriterLockSlim>();

    public static ReaderWriterLockSlim GetOrCreate<T>()
        where T : class
    {
        var entityType = typeof(T);
        if (Locks.TryGetValue(entityType, out var @lock))
        {
            return @lock;
        }
        else
        {
            var newLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
            Locks[entityType] = newLock;
            return newLock;
        }
    }
}
