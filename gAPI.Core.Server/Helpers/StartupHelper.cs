namespace gAPI.Core.Server.Helpers;

public static class StartupHelper
{
    public static void ShowApiStarted(gAPI.Core.Dtos.ServerConfig serverConfig, bool isDevelopment = false)
    {
        Console.WriteLine("##################################");
        Console.WriteLine("##                              ##");
        Console.WriteLine("##       ##       ######   ##   ##");
        Console.WriteLine("##      ####      ##   ##  ##   ##");
        Console.WriteLine("##     ##  ##     ##   ##  ##   ##");
        Console.WriteLine("##    ########    ######   ##   ##");
        Console.WriteLine("##   ##      ##   ##       ##   ##");
        Console.WriteLine("##  ##        ##  ##       ##   ##");
        Console.WriteLine("##                              ##");
        Console.WriteLine("##          JUST STARTED        ##");
        Console.WriteLine("##                              ##");
        Console.WriteLine("##################################");
        Console.WriteLine("## FRONTEND URL = " + serverConfig.FrontendUrl);
        Console.WriteLine("## USE MEMORY DATABASE = " + serverConfig.UseMemoryDatabase);
        if (isDevelopment)
        {
            Console.WriteLine("## DEFAULT CONNECTIONSTRING = " + serverConfig.DefaultConnectionString);
            Console.WriteLine("## STORAGE CONNECTIONSTRING = " + serverConfig.StorageConnectionString);
            Console.WriteLine("## FABRIC CONNECTIONSTRING = " + serverConfig.FabricConnectionString);
        }
    }
}
