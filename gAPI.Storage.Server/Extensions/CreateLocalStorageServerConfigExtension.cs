using gAPI.Storage.Server;
using Microsoft.Extensions.Configuration;

namespace gAPI.Storage.Server.Extensions;

public static class CreateLocalStorageServerConfigExtension
{
    public static LocalStorageServerConfig CreateLocalStorageServerConfig(this IConfigurationManager m)
    {
        var config = new LocalStorageServerConfig(
            m["BaseUrl"] ?? throw new Exception("BaseUrl not supplied in configuration"));
        return config;
    }
}
