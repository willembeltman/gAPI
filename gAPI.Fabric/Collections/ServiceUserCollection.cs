using gAPI.Fabric.Models;
using gAPI.Fabric.Types;
using System.Collections;
using System.Collections.Concurrent;

namespace gAPI.Fabric.Collections;

public class ServiceUserCollection : IEnumerable<ServiceUser>
{
    private readonly ConcurrentDictionary<UserId, ServiceUser> Users = new();
    public int Count => Users.Count;

    public ServiceUser GetOrCreate(UserId userId) => Users.GetOrAdd(userId, _ => new ServiceUser(userId));
    public ServiceUser? TryGet(UserId userId)
    {
        if (!Users.TryGetValue(userId, out var user))
            return null;
        return user;
    }
    public bool Remove(UserId userId) => Users.TryRemove(userId, out _);

    public IEnumerator<ServiceUser> GetEnumerator() => Users.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}