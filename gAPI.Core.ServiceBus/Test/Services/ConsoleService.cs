using UwvLlm.Infrastructure.Messaging.Interfaces;

namespace UwvLlm.Infrastructure.Messaging.Services;

public class ConsoleService : IConsoleService
{
    public async Task Start(CancellationToken ct)
    {
        Console.WriteLine("LlmProxy started");
        Console.WriteLine("Press Q to quit");

        while (true)
        {
            var key = Console.ReadKey();
            if (key.Key == ConsoleKey.Q)
                break;
        }
    }

    public void WriteLine(Exception ex)
    {
        Console.WriteLine(ex);
    }

    public void WriteLine(string text)
    {
        Console.WriteLine(text);
    }
}
