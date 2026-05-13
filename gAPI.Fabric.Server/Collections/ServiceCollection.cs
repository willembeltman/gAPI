using gAPI.Core.Ids;
using gAPI.Fabric.Server.Models;
using System.Collections;
using System.Collections.Concurrent;

namespace gAPI.Fabric.Server.Collections;

public class ServiceCollection : IEnumerable<Service>
{
    private readonly ConcurrentDictionary<ServiceId, Service> Services = new();

    public ServiceCollection(FabricManager fabricManager)
    {
        FabricManager = fabricManager;
    }

    public Service this[ServiceId serviceId]
    {
        get => Services.GetOrAdd(
            serviceId,
            serviceId => new Service(serviceId, FabricManager));
        set => Services[serviceId] = value;
    }

    public FabricManager FabricManager { get; }

    public IEnumerator<Service> GetEnumerator() => Services.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

}