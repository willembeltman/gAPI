using gAPI.Core.ServiceBus.Config;
using Microsoft.Extensions.Configuration;

namespace gAPI.Core.ServiceBus.Extensions;

public static class CreateBusConfigExtension
{
    public static ServiceBusConfig CreateBusConfig(this IConfigurationManager m)
    {
        var config = new ServiceBusConfig(
            int.TryParse(m.GetSection("MaxPackageSize").ToString(), out var MaxPackageSize) ? MaxPackageSize :null,
            m.GetSection("QueueName").ToString() ?? throw new Exception("QueueName not found in config"),
            m.GetConnectionString("ServiceBus") ?? throw new Exception("ServiceBus connectionstring not found in config"));
        return config;
    }
}