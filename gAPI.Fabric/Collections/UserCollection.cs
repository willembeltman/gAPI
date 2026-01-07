using gAPI.FabricNode.Models;
using gAPI.Ids;
using System.Collections;
using System.Collections.Concurrent;

namespace gAPI.FabricNode.Collections;

public class UserCollection : IEnumerable<User>
{
    private readonly ConcurrentDictionary<UserId, User> Users = new();

    public User this[UserId userId]
    {
        get => Users.GetOrAdd(
            userId,
            userId => new User(userId));
        set => Users[userId] = value;
    }

    public User GetOrCreate(UserId userId) => Users.GetOrAdd(userId, _ => new User(userId));
    public User? TryGet(UserId userId)
    {
        if (!Users.TryGetValue(userId, out var user))
            return null;
        return user;
    }
    public bool Remove(UserId userId) => Users.TryRemove(userId, out _);

    public IEnumerator<User> GetEnumerator() => Users.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal void TryAdd(UserId userId, FabricHost connection)
    {
        var user = Users.GetOrAdd(userId, userId => new User(userId));
        user.Subscribe(connection);
    }
    internal void TryRemove(UserId userId, FabricHost connection)
    {
        if (!Users.TryGetValue(userId, out var user)) return;
        user.Unsubscribe(connection);
        if (user?.Connections.Count == 0)
            Users.TryRemove(userId, out _);
    }

}