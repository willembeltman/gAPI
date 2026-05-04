using gAPI.FabricNode.Models;
using System.Diagnostics;

namespace gAPI.FabricNode;

public class FabricProgram
{
    public static async Task StartAsync()
    {
        gAPI.Fabric.FabricClient
        Thread.Sleep(200);

        Console.WriteLine("Getting the config...");
        var config = await FabricConfig.LoadAsync();

        //WssLoggerConfig.MinimumLevel = LogLevel.Trace;

        var textArea = new ScrollWindow("Fabric Hub", new(2, 1), new(116, 12));
        var connections = new ScrollWindow("Connections", new(2, 14), new(30, 15));
        var subscriptions = new ScrollWindow("Subscriptions", new(34, 14), new(84, 15));

        textArea.SetItems(
        [
            new ColorLine () { Text = "Server started!"},
    new ColorLine () { Text = "" },
    new ColorLine () { Text = "press q to exit..." },
    new ColorLine () { Text = "press r to restart all connections..." }
        ]);

        var screen = new Screen([textArea, connections, subscriptions]);

        Console.WriteLine("Setup server...");
        await using var server = new FabricServer(config.Port, textArea);

        Console.WriteLine("Starting server...");
        var windowwidth = 0;
        try
        {
            windowwidth = Console.WindowWidth;
        }
        catch { }
        if (windowwidth < 10)
        {
            await server.StartAsync();
            return;
        }
        _ = Task.Run(server.StartAsync);

        var width = 0;
        var height = 0;
        var dirty = false;

        server.Manager.OnUpdate += (sender, e) => { dirty = true; };

        static IEnumerable<ColorLine> Get(Service a)
        {
            return
            [
                new ColorLine() { Text = $"{a.Id} S:{a.GetSendSpeed()} R:{a.GetReceiveSpeed()}" },
        .. a.Sessions.Select(s => new ColorLine() { Text = $"- {s.Id} S:{s.GetSendSpeed()} R:{s.GetReceiveSpeed()}" })
            ];
        }

        Stopwatch sw = Stopwatch.StartNew();
        var sec = 1;

        while (true)
        {
            Thread.Sleep(40);

            if (sw.Elapsed.TotalSeconds > sec)
            {
                dirty = true;
                sec++;
            }

            if (dirty)
            {
                dirty = false;
                connections.SetItems([.. server.Manager.Connections
                    .Select(a =>
                    {
                        return new ColorLine()
                        {
                            Text = $"Node {a.Id} S:{a.GetSendSpeed()} R:{a.GetReceiveSpeed()}"
                        };
                    })]);
                subscriptions.SetItems([.. server.Manager.Services.SelectMany(Get)]);
            }

            var resized = false;
            if (Console.WindowWidth != width || Console.WindowHeight != height)
            {
                width = Console.WindowWidth;
                height = Console.WindowHeight;
                resized = true;
            }
            screen.Render(resized);

            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true).Key;
                // wait for q to exit
                if (key == ConsoleKey.Q)
                    break;
                if (key == ConsoleKey.R)
                {
                    textArea.WriteLine("Restarting, please wait");
                    textArea.WriteLine();
                    await server.DisconnectAllAsync();
                }
                if (key == ConsoleKey.C)
                {
                    textArea.WriteLine("Clearing logs");
                    textArea.WriteLine();
                    textArea.SetItems([]);
                }
                if (key == ConsoleKey.S)
                {
                    textArea.WriteLine("Showing stats");
                }
                if (key == ConsoleKey.Tab)
                {
                    screen.SelectNext();
                }
                if (key == ConsoleKey.UpArrow)
                {
                    screen.Up();
                }
                if (key == ConsoleKey.DownArrow)
                {
                    screen.Down();
                }
            }
        }
    }
}