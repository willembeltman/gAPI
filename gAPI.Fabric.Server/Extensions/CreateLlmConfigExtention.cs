using gAPI.Fabric.Server.Config;
using Microsoft.Extensions.Configuration;

namespace gAPI.Fabric.Server.Extensions;

public static class CreateFabricConfigExtention
{
    public static FabricConfig CreateFabricConfig(this IConfigurationManager m)
    {
        var config = new FabricConfig()
        {
            Port = Convert.ToInt32(m["Port"])
        };
        return config;
    }
}