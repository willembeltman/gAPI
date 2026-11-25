using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace gAPI.Core.Server.Dashboard;

public sealed class NormalConsoleOutput(
    ILoggerFactory loggerFactory,
    ConsoleBuffer ConsoleBuffer)
    : IHostedService
{
    private readonly ILogger Logger = loggerFactory.CreateLogger<NormalConsoleOutput>();
    private CancellationTokenSource? Cts;

    public async Task StartAsync(CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            Cts = new CancellationTokenSource();
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(40, Cts.Token);

                    while (ConsoleBuffer.Lines.Reader.TryRead(out var line))
                    {
                        ct.ThrowIfCancellationRequested();

                        try
                        {
                            Console.WriteLine(SanitizeForConsoleFormat(line.Text), line.Color);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, "StartAsync");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "StartAsync");
            }
            finally
            {
                if (Cts != null)
                {
                    await Cts.CancelAsync();
                    Cts.Dispose();
                }
            }
        }, ct);
    }

    public async Task StopAsync(CancellationToken ct)
    {
        if (Cts == null)
            return;
        await Cts.CancelAsync();
        Cts.Dispose();
        Cts = null;
    }
    public static string SanitizeForConsoleFormat(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        return input.Replace("{", "{{").Replace("}", "}}");
    }
}