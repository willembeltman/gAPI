using gAPI.Core.Client.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace gAPI.Core.Client.Extensions;

public static class CreateServerConfigExtension
{
    public static ClientConfig CreateClientConfig(this IConfigurationManager m)
    {
        var logLevel = m.GetValue<LogLevel>("Logging:LogLevel:gAPI", LogLevel.Error);
        var config = new ClientConfig(
            m["ApiBackendUrl"] ?? "",
            m["WssBackendUrl"] ?? "",
            logLevel);
        return config;
    }
}
