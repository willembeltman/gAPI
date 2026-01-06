using gAPI.FabricNode.Models;
using gAPI.Types;
using System.Collections;
using System.Collections.Concurrent;

namespace gAPI.FabricNode.Collections;

public class ServiceCollection : IEnumerable<Service>
{
    private readonly ConcurrentDictionary<ServiceId, Service> Services = new();

    public Service this[ServiceId serviceId]
    {
        get => Services.GetOrAdd(
            serviceId, 
            serviceId => new Service(serviceId));
        set => Services[serviceId] = value;
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