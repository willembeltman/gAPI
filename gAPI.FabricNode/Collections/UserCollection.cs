using gAPI.FabricNode.Models;
using gAPI.Ids;
using System.Collections.Concurrent;

namespace gAPI.FabricNode.Collections;

public class UserCollection
{
    private readonly ConcurrentDictionary<UserId, User> Users = new();

    public User? TryGet(UserId userId)
    {
        if (!Users.TryGetValue(userId, out var user))
            return null;
        return user;
    }

    public void TryAdd(UserId userId, FabricHost connection)
    {
        var user = Users.GetOrAdd(userId, userId => new User(userId));
        user.Subscribe(connection);
    }
    public void TryRemove(UserId userId, FabricHost connection)
    {
        if (!Users.TryGetValue(userId, out var user)) return;
        user.Unsubscribe(connection);
        if (user?.Connections.Count == 0)
            Users.TryRemove(userId, out _);
    }
}