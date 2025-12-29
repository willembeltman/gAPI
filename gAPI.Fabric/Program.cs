using gAPI.Fabric;

Console.WriteLine("Getting the config");
var config = await Config.LoadAsync();

Console.WriteLine("Setup server");
await using var server = new BusServer(config.Port);

Console.WriteLine("Starting server");
_ = server.StartAsync();

Console.WriteLine("Server started, press q to exit...");
while (Console.ReadKey(true).Key != ConsoleKey.Q)
{
    // wait for q to exit
}