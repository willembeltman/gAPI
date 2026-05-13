using gAPI.Core.ServiceBus.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace gAPI.Core.ServiceBus.Extensions;

public static class AppStartConsoleWithServiceBusExtention
{
    public static async Task StartConsoleWithServiceBusAsync(this IHost app)
    {
        using var scope = app.Services.CreateScope();

        var workerService = scope.ServiceProvider.GetRequiredService<IServiceBusReceiver>();
        var consoleService = scope.ServiceProvider.GetRequiredService<IConsoleService>();

        using var cts = new CancellationTokenSource();

        var workerTask = workerService.StartAsync("LlmProxy", cts.Token);
        var consoleTask = consoleService.Start(cts.Token);
        await Task.WhenAny(workerTask, consoleTask);

        cts.Cancel();

        if (workerTask.Exception != null)
            throw workerTask.Exception;

        await Task.WhenAll(consoleTask);
    }
}
