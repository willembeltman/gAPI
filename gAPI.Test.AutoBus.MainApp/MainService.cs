using gAPI.Generated;
using Microsoft.Extensions.Hosting;

namespace gAPI.Test.AutoBus.MainApp;

public class MainService(
    IBusContext busContext)
    : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        await Task.Delay(5000);

        var duration = busContext.Service1.ToAny().Ping(ct);
        await foreach(var item in duration)
        {
            Console.WriteLine($"{item}ms");
        }
        //await busContext.Service1.ToAny().WriteLineAsync("jdsbaksjd", ct); // gaat goed.

        //CancellationTokenSource cts = new();
        //cts.CancelAfter(TimeSpan.FromSeconds(100));

        ////var list = busContext.Service1.ToAny().Get("");
        ////await foreach (var item in list)
        ////{
        ////} // Deze gaat ook goed

        //TimeSpan.FromDays(1);

        //async IAsyncEnumerable<string> test()
        //{
        //    yield return "Hoi hoi";
        //    yield return "Hoi hoi";
        //    yield return "Hoi hoi";
        //    yield return "Hoi hoi";
        //    yield return "Hoi hoi";
        //    yield return "Hoi hoi";
        //    yield return "Hoi hoi";
        //}
        //;
        //await busContext.Service1.ToAny().Test(test(), ct); 
        //// Deze komt aan, doet 1 yield, en valt er dan doorheen de functie uit.
        //// Ik heb zelfs het gevoel dat hij eruit valt, wat de enumerator disposed en daardoor helemaal stopt,
        //// want ik heb gezien dat hij soms ook niet eens 1 yield doet.
    }

    public async Task StopAsync(CancellationToken ct)
    {
    }
}
