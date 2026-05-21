namespace gAPI.Storage.Server.EntityFrameworkDisk.Interfaces;

public interface IDiskSet<T> : IDiskSet, ILongCollection<T>, IKeyedSet<T>
{
    void Update(List<ChangedEntity<T>> changedEntities);
}

public interface IDiskSet : IKeyedSet, ILongCollection
{
    bool RemoveRange(IEnumerable<object> keys);
}