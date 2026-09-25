using gAPI.Fabric.Server.Config;
using gAPI.Fabric.Server.ConsoleHelper;
using gAPI.Fabric.Server.Services;

namespace gAPI.Fabric.Server;

public static class FabricProgram
{
    public static async Task StartAsync(int port = 9494, CancellationToken cancellationToken = default)
    {
        var config = new FabricConfig { Port = port };

        if (!CanRenderConsole())
        {
            await using var server = new FabricServer(config.Port);
            await server.StartAsync();
            return;
        }

        var dashboard = new FabricConsoleDashboard();
        await using var interactiveServer = new FabricServer(config.Port, dashboard);
        var serverTask = interactiveServer.StartAsync();

        await dashboard.RunAsync(interactiveServer, cancellationToken);
        await serverTask;
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
