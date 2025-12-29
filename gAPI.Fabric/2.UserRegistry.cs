using System.Collections;
using System.Collections.Concurrent;

namespace gAPI.Fabric;

public class UserRegistry : IEnumerable<User>
{
    private readonly ConcurrentDictionary<UserId, User> Users = new();
    public int Count => Users.Count;

    public User GetOrCreate(UserId userId)
    {
        return Users.GetOrAdd(userId, _ => new User(userId));
    }

    public User? TryGet(UserId userId)
    {
        if (!Users.TryGetValue(userId, out var user))
            return null;
        return user;
    }

    public bool Remove(UserId userId)
    {
        return Users.TryRemove(userId, out _);
    }

    public IEnumerator<User> GetEnumerator() => Users.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}