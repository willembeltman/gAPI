using System.Collections;
using System.Collections.Concurrent;

namespace gAPI.Fabric;

public class ServiceRegistry : IEnumerable<Service>
{
    private readonly ConcurrentDictionary<ServiceId, Service> Services = new();
    public int Count => Services.Count;

    public Service GetOrCreate(ServiceId serviceId)
    {
        return Services.GetOrAdd(serviceId, _ => new Service(serviceId));
    }

    public Service? TryGet(ServiceId serviceId)
    {
        if (!Services.TryGetValue(serviceId, out var service))
            return null;
        return service;
    }

    public bool Remove(ServiceId serviceId)
    {
        return Services.TryRemove(serviceId, out _);
    }

    public IEnumerator<Service> GetEnumerator() => Services.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}