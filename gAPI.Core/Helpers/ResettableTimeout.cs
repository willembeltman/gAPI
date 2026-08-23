namespace gAPI.Core.Helpers;

public sealed class ResettableTimeout : IDisposable
{
    private readonly TimeSpan _timeout;
    private readonly Action _onTimeout;

    private readonly object _lock = new();
    private CancellationTokenSource _cts;
    private bool _disposed;

    public ResettableTimeout(TimeSpan timeoutDuration, Action onTimeout)
    {
        _timeout = timeoutDuration;
        _onTimeout = onTimeout;

        _cts = new CancellationTokenSource();
        _ = RunAsync(_cts.Token);
    }

    public void Reset()
    {
        lock (_lock)
        {
            if (_disposed)
                return;

            _cts.Cancel();
            _cts.Dispose();

            _cts = new CancellationTokenSource();
            _ = RunAsync(_cts.Token);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(_timeout, ct);

            if (!ct.IsCancellationRequested)
                _onTimeout();
        }
        catch (OperationCanceledException)
        {
            // Timer werd gereset of disposed.
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
                return;

            _disposed = true;

            _cts.Cancel();
            _cts.Dispose();
        }
    }
}