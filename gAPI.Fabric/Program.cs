using gAPI.Fabric;
using gAPI.Fabric.Models;

Console.WriteLine("Getting the config");
var config = await Config.LoadAsync();

Console.WriteLine("Setup server");
await using var server = new Server(config.Port);

Console.WriteLine("Starting server");
_ = server.StartAsync();

Console.WriteLine("Server started, press q to exit...");
while (Console.ReadKey(true).Key != ConsoleKey.Q)
{
    // wait for q to exit
}