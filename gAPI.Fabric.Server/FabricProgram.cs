using gAPI.Fabric.Server.Config;
using gAPI.Fabric.Server.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace gAPI.Fabric.Server;

public static class FabricProgram
{
    public static async Task StartAsync(int port = 9494, CancellationToken ct = default)
    {
        var config = new FabricConfig { Port = port };
        await StartAsync(config, ct);
    }
    public static async Task StartAsync(FabricConfig config, CancellationToken ct = default)
    {
        var builder = Host.CreateApplicationBuilder();
#if DEBUG
        using var stream = File.OpenRead("appsettings.Development.json");
        if (stream != null)
            builder.Configuration.AddJsonStream(stream);
#endif

        builder.AddFabric();
        var app = builder.Build();
        await app.RunAsync();
    }
}
