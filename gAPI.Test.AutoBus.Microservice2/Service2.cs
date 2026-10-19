using Microsoft.Extensions.Hosting;
using gAPI.Test.AutoBus.Shared;

namespace gAPI.Test.AutoBus.Microservice2;

public class Service2 : IService2, IHostedService
{
    public async IAsyncEnumerable<string> Get(string name)
    {
        yield return "Hoi from 2";
        
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
    }

    public Task Test(IAsyncEnumerable<string> test)
    {
        throw new NotImplementedException();
    }

    public void WriteLine(string message)
    {
        Console.WriteLine($"From Microservice 2: {message}");
    }

    public async Task WriteLineAsync(string message)
    {
        Console.WriteLine($"From Microservice 2: {message}");
    }
}
