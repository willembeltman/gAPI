

namespace gAPI.Helpers;

public static class StartupHelper
{

    public static void ShowStorageStarted(string? userName)
    {
        Console.WriteLine("#########################################################");
        Console.WriteLine("##                                                     ##");
        Console.WriteLine("##  ###  ######  ####   ####      ###     ####   ##### ##");
        Console.WriteLine("## ## ##   ##   ##  ##  ##  ##   ## ##   ##  ##  ##    ##");
        Console.WriteLine("## ##      ##   ##  ##  ##  ##  ##   ##  ##      ##    ##");
        Console.WriteLine("##  ###    ##   ##  ##  #####   #######  ## #### ####  ##");
        Console.WriteLine("##    ##   ##   ##  ##  ## ##   ##   ##  ##  ##  ##    ##");
        Console.WriteLine("## ## ##   ##   ##  ##  ##  ##  ##   ##  ##  ##  ##    ##");
        Console.WriteLine("##  ###    ##    ####   ##  ##  ##   ##   ####   ##### ##");
        Console.WriteLine("##                                                     ##");
        Console.WriteLine("#########################################################");
        Console.WriteLine($"## gAPI.Storage.Server.WebApplicationBuilderExtension UserName = {userName}");
    }
}