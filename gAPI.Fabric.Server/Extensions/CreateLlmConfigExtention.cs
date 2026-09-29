using gAPI.Fabric.Server.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace gAPI.Fabric.Server.Extensions;

public static class CreateFabricConfigExtention
{
    public static FabricConfig CreateFabricConfig(this IConfigurationManager m)
    {
        var logLevel = LogLevel.Error;
        if (Enum.TryParse<LogLevel>(m["Logging__LogLevel__gAPI"], true, out var logLevel2))
        {
            logLevel = logLevel2;
        }
        var config = new FabricConfig()
        {
            Port = Convert.ToInt32(m["Port"]),
            LogLevel = logLevel
        };
        return config;
    }
}