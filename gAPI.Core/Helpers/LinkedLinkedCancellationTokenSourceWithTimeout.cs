namespace gAPI.Core.Helpers;

public sealed class LinkedCancellationTokenSourceWithTimeout : IDisposable
{
    private readonly CancellationTokenSource _cts;
    private readonly ResettableTimeout _timeout;

    private bool _disposed;
    private readonly object _lock = new();

    public LinkedCancellationTokenSourceWithTimeout(
        TimeSpan timeoutDuration,
        CancellationToken ct = default)
    {
        _cts = ct.CanBeCanceled
            ? CancellationTokenSource.CreateLinkedTokenSource(ct)
            : new CancellationTokenSource();

        _timeout = new ResettableTimeout(
            timeoutDuration,
            Cancel);
    }

    public CancellationToken Token => _cts.Token;

    public CancellationTokenSource Cts => _cts;

    public ResettableTimeout Timeout => _timeout;

    public void Cancel()
    {
        lock (_lock)
        {
            if (_disposed)
                return;

            _cts.Cancel();
        }
    }
    public void Reset()
    {
        lock (_lock)
        {
            if (_disposed)
                return;

            _timeout.Reset();
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
                return;

            _disposed = true;

            _timeout.Dispose();
            _cts.Dispose();
        }
    }
}