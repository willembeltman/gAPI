using gAPI.Generated;
using Microsoft.Extensions.Hosting;

namespace gAPI.Test.AutoBus.MainApp;

public class MainService(
    IBusContext busContext)
    : IHostedService
{

    public async Task StartAsync(CancellationToken ct)
    {
        async IAsyncEnumerable<string> test()
        {
            yield return "Hoi hoi";
        };
        await Task.Delay(5000);
        await busContext.Service1.ToAny.Test(test());
        await Task.Delay(5000);
        await busContext.Service2.ToAny.Test(test());
    }

    public async Task StopAsync(CancellationToken ct)
    {
    }

}
