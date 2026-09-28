using Microsoft.Extensions.Hosting;

namespace gAPI.Fabric.Server.Services;

internal sealed class NormalConsoleOutput(ConsoleBuffer ConsoleBuffer) : IHostedService
{
    private CancellationTokenSource? Cts;

    public async Task StartAsync(CancellationToken ct2)
    {
        Cts = CancellationTokenSource.CreateLinkedTokenSource(ct2);
        var ct = Cts.Token;
        _ = Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(40, ct);

                while (ConsoleBuffer.Lines.TryDequeue(out var line))
                {
                    Console.WriteLine(line.Text, line.Color);
                }
            }
        }, ct);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (Cts == null)
            return;
        await Cts.CancelAsync();
        Cts.Dispose();
    }
}
