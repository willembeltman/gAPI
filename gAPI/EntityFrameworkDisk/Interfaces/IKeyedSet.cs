using System;

namespace gAPI.EntityFrameworkDisk.Interfaces;

public interface IKeyedSet<T> : IKeyedSet
{
    object? GetKeyValue(T item);
}
public interface IKeyedSet
{
    string? KeyName { get; }
    Type? KeyType { get; }
}