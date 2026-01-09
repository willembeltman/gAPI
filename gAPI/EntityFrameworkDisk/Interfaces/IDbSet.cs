using System.Collections.Generic;

namespace gAPI.EntityFrameworkDisk.Interfaces;

public interface IDbSet<T> : IDbSet, ILongCollection<T>, IKeyedSet<T>
{
}

public interface IDbSet : IKeyedSet, ILongCollection
{
    IEnumerable<object> GetAddedEntities();
    IEnumerable<ChangedEntityObject> GetChangedEntities();
    IEnumerable<object> GetRemoveEntities();

    void SaveChanges();
}