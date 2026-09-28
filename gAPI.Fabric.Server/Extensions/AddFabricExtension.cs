using gAPI.Fabric.Server.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace gAPI.Fabric.Server.Extensions;

public static class AddFabricExtension
{
    public static void AddFabric(this IHostApplicationBuilder builder, bool useConsoleDashboard = true)
    {
        var config = builder.Configuration.CreateFabricConfig();
        builder.Services.AddSingleton(config);

        var consoleBuffer = new ConsoleBuffer();
        builder.Services.AddSingleton(consoleBuffer);

        var manager = new FabricManager(config, consoleBuffer);
        builder.Services.AddSingleton(manager);

        // Hosted services starten
        builder.Services.AddHostedService<FabricServer>();
        if (useConsoleDashboard && CanRenderConsole())
            builder.Services.AddHostedService<DashboardConsoleOutput>();
        else
            builder.Services.AddHostedService<NormalConsoleOutput>();
    }

    private static bool CanRenderConsole()
    {
        try
        {
            return !Console.IsOutputRedirected && Console.WindowWidth >= 10;
        }
        catch (IOException)
        {
            return false;
        }
    }
}
