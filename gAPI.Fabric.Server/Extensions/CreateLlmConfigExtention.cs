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
        var portStr = m["Port"];
        int port = 9494;
        if (int.TryParse(portStr, out var p) && p > 0)
        {
            port = p;
        }
        var config = new FabricConfig()
        {
            Port = port,
            LogLevel = logLevel
        };
        return config;
    }
}