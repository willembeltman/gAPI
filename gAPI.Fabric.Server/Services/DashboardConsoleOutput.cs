//using gAPI.Fabric.Server.Config;
//using gAPI.Fabric.Server.Models;
//using gAPI.Fabric.Server.Monitoring;
//using Microsoft.Extensions.Hosting;
//using System.Diagnostics;

//namespace gAPI.Fabric.Server.Services;

///// <summary>
///// Terminal-specific presentation and input. The runtime exposes snapshots instead of UI objects.
///// </summary>
//public sealed class DashboardConsoleOutput : IHostedService
//{
//    private readonly ScrollWindow LogWindow = new("Fabric Hub", new(2, 1), new(116, 12));
//    private readonly ScrollWindow ConnectionsWindow = new("Connections", new(2, 14), new(30, 15));
//    private readonly ScrollWindow SubscriptionsWindow = new("Subscriptions", new(34, 14), new(84, 15));

//    public FabricConfig Config { get; }

//    private readonly ConsoleBuffer ConsoleBuffer;
//    private readonly FabricManager Manager;

//    private bool Dirty = true;
//    private CancellationTokenSource? Cts;

//    public DashboardConsoleOutput(
//        FabricConfig config,
//        ConsoleBuffer consoleBuffer,
//        FabricManager manager)
//    {
//        Config = config;
//        ConsoleBuffer = consoleBuffer;
//        Manager = manager;
//        LogWindow.SetItems(
//        [
//            new ColorLine { Text = "Server started!" },
//            new ColorLine { Text = "" },
//            new ColorLine { Text = "press q to exit..." },
//            new ColorLine { Text = "press r to restart all connections..." },
//            new ColorLine { Text = "" },
//            new ColorLine { Text = $"Listening to port {Config.Port}" },
//        ]);
//    }

//    public async Task StartAsync(CancellationToken ct2)
//    {
//        if (CanRenderConsole() == false)
//            return;

//        Cts = CancellationTokenSource.CreateLinkedTokenSource(ct2);
//        var ct = Cts.Token;

//        _ = Task.Run(async () =>
//        {
//            var screen = new Screen([LogWindow, ConnectionsWindow, SubscriptionsWindow]);
//            var width = 0;
//            var height = 0;
//            var stopwatch = Stopwatch.StartNew();
//            var nextRefresh = 0;

//            //void OnChanged(object? sender, EventArgs eventArgs) => Dirty = true;
//            //Manager.Changed += OnChanged;

//            try
//            {
//                while (!ct.IsCancellationRequested)
//                {
//                    await Task.Delay(40, ct);

//                    if (stopwatch.Elapsed.TotalSeconds >= nextRefresh)
//                    {
//                        Dirty = true;
//                        nextRefresh++;
//                    }

//                    if (Dirty)
//                    {
//                        Dirty = false;
//                        RenderSnapshot(Manager.GetDashboardSnapshot());
//                    }

//                    while (ConsoleBuffer.Lines.TryDequeue(out var line))
//                    {
//                        LogWindow.WriteLine(line.Text, line.Color);
//                    }

//                    var resized = Console.WindowWidth != width || Console.WindowHeight != height;
//                    if (resized)
//                    {
//                        width = Console.WindowWidth;
//                        height = Console.WindowHeight;
//                    }
//                    screen.Render(resized);

//                    if (Console.KeyAvailable && await HandleKeyAsync(screen, Console.ReadKey(true).Key))
//                        break;
//                }
//            }
//            catch (OperationCanceledException) when (ct.IsCancellationRequested)
//            {
//            }
//            finally
//            {
//                //Manager.Changed -= OnChanged;
//                await Cts.CancelAsync();
//                Cts.Dispose();
//                Cts = null;
//            }
//        }, ct);
//    }

//    public async Task StopAsync(CancellationToken cancellationToken)
//    {
//        if (Cts == null)
//            return;
//        await Cts.CancelAsync();
//        Cts.Dispose();
//    }

//    private static bool CanRenderConsole()
//    {
//        try
//        {
//            return !Console.IsOutputRedirected && Console.WindowWidth >= 10;
//        }
//        catch (IOException)
//        {
//            return false;
//        }
//    }

//    public void WriteInfo(string message) => LogWindow.WriteLine(message);

//    public void WriteError(string message, Exception? exception = null)
//    {
//        LogWindow.WriteLine(message, ConsoleColor.Red);
//        if (exception is not null)
//            LogWindow.WriteLine(exception.ToString(), ConsoleColor.Red);
//    }

//    private void RenderSnapshot(FabricDashboardSnapshot snapshot)
//    {
//        ConnectionsWindow.SetItems(
//        [
//            .. snapshot.Connections.Select(connection => new ColorLine
//            {
//                Text = $"Node {connection.ConnectionId} S:{FormatSpeed(connection.SentBytesPerSecond)} R:{FormatSpeed(connection.ReceivedBytesPerSecond)}"
//            })
//        ]);

//        SubscriptionsWindow.SetItems(
//        [
//            .. snapshot.Services.Select(service => new ColorLine
//            {
//                Text = $"{service.ServiceId} S:{FormatSpeed(service.SentBytesPerSecond)} R:{FormatSpeed(service.ReceivedBytesPerSecond)} {service.SessionCount} sessions {service.UserCount} users"
//            })
//        ]);
//    }

//    private async Task<bool> HandleKeyAsync(Screen screen, ConsoleKey key)
//    {
//        switch (key)
//        {
//            case ConsoleKey.Q:
//                return true;
//            case ConsoleKey.R:
//                WriteInfo("Restarting, please wait");
//                await Manager.DisconnectAllAsync();
//                break;
//            case ConsoleKey.C:
//                LogWindow.SetItems([]);
//                break;
//            case ConsoleKey.S:
//                WriteInfo("Showing stats");
//                break;
//            case ConsoleKey.Tab:
//                screen.SelectNext();
//                break;
//            case ConsoleKey.UpArrow:
//                screen.Up();
//                break;
//            case ConsoleKey.DownArrow:
//                screen.Down();
//                break;
//        }

//        return false;
//    }

//    private static string FormatSpeed(long bytes) => bytes switch
//    {
//        < 1024 => $"{bytes}b/sec",
//        < 1024 * 1024 => $"{bytes / 1024}kb/sec",
//        < 1024L * 1024 * 1024 => $"{bytes / (1024 * 1024)}mb/sec",
//        < 1024L * 1024 * 1024 * 1024 => $"{bytes / (1024L * 1024 * 1024)}gb/sec",
//        _ => $"{bytes / (1024L * 1024 * 1024 * 1024)}tb/sec"
//    };
//}
