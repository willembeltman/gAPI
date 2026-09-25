using gAPI.Fabric.Server.Interfaces;
using gAPI.Fabric.Server.Monitoring;
using gAPI.Fabric.Server.Services;
using System.Diagnostics;

namespace gAPI.Fabric.Server.ConsoleHelper;

/// <summary>
/// Terminal-specific presentation and input. The runtime exposes snapshots instead of UI objects.
/// </summary>
public sealed class FabricConsoleDashboard : IFabricStatusSink
{
    private readonly ScrollWindow Log = new("Fabric Hub", new(2, 1), new(116, 12));
    private readonly ScrollWindow Connections = new("Connections", new(2, 14), new(30, 15));
    private readonly ScrollWindow Subscriptions = new("Subscriptions", new(34, 14), new(84, 15));
    private bool Dirty = true;

    public FabricConsoleDashboard()
    {
        Log.SetItems(
        [
            new ColorLine { Text = "Server started!" },
            new ColorLine { Text = "" },
            new ColorLine { Text = "press q to exit..." },
            new ColorLine { Text = "press r to restart all connections..." }
        ]);
    }

    public async Task RunAsync(FabricServer server, CancellationToken cancellationToken = default)
    {
        var screen = new Screen([Log, Connections, Subscriptions]);
        var width = 0;
        var height = 0;
        var stopwatch = Stopwatch.StartNew();
        var nextRefresh = 0;

        void OnChanged(object? sender, EventArgs eventArgs) => Dirty = true;
        server.Manager.Changed += OnChanged;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(40, cancellationToken);

                if (stopwatch.Elapsed.TotalSeconds >= nextRefresh)
                {
                    Dirty = true;
                    nextRefresh++;
                }

                if (Dirty)
                {
                    Dirty = false;
                    RenderSnapshot(server.GetDashboardSnapshot());
                }

                var resized = Console.WindowWidth != width || Console.WindowHeight != height;
                if (resized)
                {
                    width = Console.WindowWidth;
                    height = Console.WindowHeight;
                }

                screen.Render(resized);

                if (Console.KeyAvailable && await HandleKeyAsync(server, screen, Console.ReadKey(true).Key))
                    break;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            server.Manager.Changed -= OnChanged;
            await server.StopAsync();
        }
    }

    public void WriteInfo(string message) => Log.WriteLine(message);

    public void WriteError(string message, Exception? exception = null)
    {
        Log.WriteLine(message, ConsoleColor.Red);
        if (exception is not null)
            Log.WriteLine(exception.ToString(), ConsoleColor.Red);
    }

    private void RenderSnapshot(FabricDashboardSnapshot snapshot)
    {
        Connections.SetItems(
        [
            .. snapshot.Connections.Select(connection => new ColorLine
            {
                Text = $"Node {connection.ConnectionId} S:{FormatSpeed(connection.SentBytesPerSecond)} R:{FormatSpeed(connection.ReceivedBytesPerSecond)}"
            })
        ]);

        Subscriptions.SetItems(
        [
            .. snapshot.Services.Select(service => new ColorLine
            {
                Text = $"{service.ServiceId} S:{FormatSpeed(service.SentBytesPerSecond)} R:{FormatSpeed(service.ReceivedBytesPerSecond)} {service.SessionCount} sessions {service.UserCount} users"
            })
        ]);
    }

    private async Task<bool> HandleKeyAsync(FabricServer server, Screen screen, ConsoleKey key)
    {
        switch (key)
        {
            case ConsoleKey.Q:
                return true;
            case ConsoleKey.R:
                WriteInfo("Restarting, please wait");
                await server.DisconnectAllAsync();
                break;
            case ConsoleKey.C:
                Log.SetItems([]);
                break;
            case ConsoleKey.S:
                WriteInfo("Showing stats");
                break;
            case ConsoleKey.Tab:
                screen.SelectNext();
                break;
            case ConsoleKey.UpArrow:
                screen.Up();
                break;
            case ConsoleKey.DownArrow:
                screen.Down();
                break;
        }

        return false;
    }

    private static string FormatSpeed(long bytes) => bytes switch
    {
        < 1024 => $"{bytes}b/sec",
        < 1024 * 1024 => $"{bytes / 1024}kb/sec",
        < 1024L * 1024 * 1024 => $"{bytes / (1024 * 1024)}mb/sec",
        < 1024L * 1024 * 1024 * 1024 => $"{bytes / (1024L * 1024 * 1024)}gb/sec",
        _ => $"{bytes / (1024L * 1024 * 1024 * 1024)}tb/sec"
    };
}
