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

        // Gebruik .Value of GetValue<string>() in plaats van .ToString() op de sectie
        //var queueName = m.GetSection("QueueName").Value
        //    ?? throw new Exception("QueueName not found in config");

        // Aspire vult dit nu automatisch in omdat de component "ServiceBus" heet in de AppHost
        var connectionString = m.GetConnectionString("ServiceBus")
            ?? throw new Exception("ServiceBus connectionstring not found in config");

        return new ServiceBusConfig(
            maxPackageSize,
            //queueName, 
            connectionString);
    }
}
