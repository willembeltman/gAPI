using gAPI.Fabric.Server.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace gAPI.Fabric.Server.Extensions;

public static class CreateFabricConfigExtention
{
    public static FabricConfig CreateFabricConfig(this IConfigurationManager m)
    {
        var logLevel = LogLevel.Error;
        if (Enum.TryParse<LogLevel>(m["Logging:LogLevel:gAPI"], true, out var logLevel2))
        {
            logLevel = logLevel2;
        }
        var portStr = m["Port"];
        int port = 30494;
        if (int.TryParse(portStr, out var p) && p > 0)
        {
            port = p;
        }
        var busPortStr = m["BusPort"];
        int busPort = 30595;
        if (int.TryParse(busPortStr, out var bp) && bp > 0)
        {
            busPort = bp;
        }
        var planePortStr = m["PlanePort"];
        int planePort = 30696;
        if (int.TryParse(planePortStr, out var pp) && pp > 0)
        {
            planePort = pp;
        }
        var config = new FabricConfig()
        {
            Port = port,
            BusPort = busPort,
            PlanePort = planePort,
            LogLevel = logLevel
        };
        return config;
    }
}