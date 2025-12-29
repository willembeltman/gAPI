using gAPI.Fabric.Models;
using gAPI.Fabric.Types;
using System.Collections;
using System.Collections.Concurrent;

namespace gAPI.Fabric.Collections;

public class ServiceScopeCollection : IEnumerable<ServiceScope>
{
    private readonly ConcurrentDictionary<ScopeId, ServiceScope> Scopes = new();
    public int Count => Scopes.Count;

    public ServiceScope GetOrCreate(ScopeId scopeId) => Scopes.GetOrAdd(scopeId, _ => new ServiceScope(scopeId));
    public ServiceScope? TryGet(ScopeId scopeId)
    {
        if (!Scopes.TryGetValue(scopeId, out var scope))
            return null;
        return scope;
    }
    public bool Remove(ScopeId scopeId) => Scopes.TryRemove(scopeId, out _);

    public IEnumerator<ServiceScope> GetEnumerator() => Scopes.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}