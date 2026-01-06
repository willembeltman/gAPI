using Azure.Core.Pipeline;
using gAPI.FabricNode;
using gAPI.FabricNode.Models;

Console.WriteLine("Getting the config");
var config = await Config.LoadAsync();

Console.WriteLine("Setup server");
await using var server = new Server(config.Port);

Console.WriteLine("Starting server");
_ = server.StartAsync();

Console.WriteLine("Server started!");
Console.WriteLine();
Console.WriteLine("press q to exit...");
Console.WriteLine("press r to restart all connections...");
Console.WriteLine();
while (true)
{
    var key = Console.ReadKey(true).Key;
    // wait for q to exit
    if (key == ConsoleKey.Q)
        break;
    if (key == ConsoleKey.R)
    {
        Console.Write("Restarting, please wait");
        Console.WriteLine();
        await server.DisconnectAll();
    }
}