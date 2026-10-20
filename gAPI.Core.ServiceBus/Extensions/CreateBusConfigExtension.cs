using gAPI.Core.ServiceBus.Config;
using Microsoft.Extensions.Configuration;

namespace gAPI.Core.ServiceBus.Extensions;

public static class CreateBusConfigExtension
{
    public static ServiceBusConfig CreateBusConfig(this IConfiguration m) // Veranderd naar IConfiguration voor bredere ondersteuning
    {
        // Gebruik .GetValue<int?> of int.TryParse op de stringwaarde
        var maxPackageSizeStr = m.GetSection("MaxPackageSize").Value;
        int? maxPackageSize = int.TryParse(maxPackageSizeStr, out var parsedSize) ? parsedSize : null;

        var queueName = m.GetSection("QueueName").Value ?? "Standard";

        // Aspire vult dit nu automatisch in omdat de component "ServiceBus" heet in de AppHost
        var connectionString = m.GetConnectionString("ServiceBus")
            ?? m.GetConnectionString("Fabric")
            ?? m.GetConnectionString("fabric")
            ?? m.GetSection("FabricConnectionString").Value
            ?? throw new Exception("ServiceBus connectionstring not found in config");

        return new ServiceBusConfig(
            maxPackageSize,
            queueName,
            connectionString);
    }
}
