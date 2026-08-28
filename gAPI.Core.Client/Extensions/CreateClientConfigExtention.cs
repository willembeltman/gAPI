using gAPI.Core.Dtos;
using Microsoft.Extensions.Configuration;

namespace gAPI.Core.Client.Extensions;

public static class CreateServerConfigExtension
{
    public static ClientConfig CreateClientConfig(this IConfigurationManager m)
    {
        var config = new ClientConfig(
            m["ApiBackendUrl"] ?? "",
            m["WssBackendUrl"] ?? "");
        return config;
    }
}
