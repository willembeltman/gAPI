using gAPI.Core.ServiceBus.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace gAPI.Core.ServiceBus.Extensions;

public static class AppRunWithServiceBusExtension
{
    public static void RunWithServiceBus(this IHost app, string busName)
    {
        using CancellationTokenSource cts = new CancellationTokenSource();
        using var scope = app.Services.CreateScope();
        var receiverService = scope.ServiceProvider.GetRequiredService<IServiceBusReceiver>();
        var receiverTask = Task.Run(async () => await receiverService.StartAsync(busName, cts.Token));
        app.Run();
        cts.Cancel();
        receiverTask.GetAwaiter().GetResult();
    }
}
