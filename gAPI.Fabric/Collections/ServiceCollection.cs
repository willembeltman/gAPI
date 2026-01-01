using gAPI.Fabric.Models;
using gAPI.Types;
using System.Collections;
using System.Collections.Concurrent;

namespace gAPI.Fabric.Collections;

public class ServiceCollection : IEnumerable<Service>
{
    private readonly ConcurrentDictionary<ServiceId, Service> Services = new();
    public int Count => Services.Count;

    public Service GetOrCreate(ServiceId serviceId) => Services.GetOrAdd(serviceId, _ => new Service(serviceId));
    public Service? TryGet(ServiceId serviceId)
    {
        if (!Services.TryGetValue(serviceId, out var service))
            return null;
        return service;
    }
    public bool Remove(ServiceId serviceId) => Services.TryRemove(serviceId, out _);

    public IEnumerator<Service> GetEnumerator() => Services.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

}