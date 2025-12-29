using gAPI.Fabric.Models;
using gAPI.Fabric.Types;
using System.Collections;
using System.Collections.Concurrent;

namespace gAPI.Fabric.Collections;

public class ScopeCollection : IEnumerable<Scope>
{
    private readonly ConcurrentDictionary<ScopeId, Scope> Scopes = new();
    public int Count => Scopes.Count;

    public Scope GetOrCreate(ScopeId scopeId) => Scopes.GetOrAdd(scopeId, _ => new Scope(scopeId));
    public Scope? TryGet(ScopeId scopeId)
    {
        if (!Scopes.TryGetValue(scopeId, out var scope))
            return null;
        return scope;
    }
    public bool Remove(ScopeId scopeId) => Scopes.TryRemove(scopeId, out _);

    public IEnumerator<Scope> GetEnumerator() => Scopes.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}