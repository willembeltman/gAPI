namespace gAPI.Core.Helpers;

public sealed class LinkedCancellationTokenSourceWithTimeout : IDisposable
{
    private readonly CancellationTokenSource _cts;
    private readonly ResettableTimeout _timeout;

    private bool _disposed;
    private readonly object _lock = new();

    public Action? OnTimeout { get; set; }
    public Action? OnDispose { get; set; }

    public LinkedCancellationTokenSourceWithTimeout(
        TimeSpan timeoutDuration,
        CancellationToken ct)
    {
        _cts = ct.CanBeCanceled
            ? CancellationTokenSource.CreateLinkedTokenSource(ct)
            : new CancellationTokenSource();

        _timeout = new ResettableTimeout(
            timeoutDuration,
            Cancel);
    }
    public LinkedCancellationTokenSourceWithTimeout(
        TimeSpan timeoutDuration,
        Action onTimeout,
        CancellationToken ct) : this(timeoutDuration, ct)
    {
        OnTimeout = onTimeout;
    }
    public LinkedCancellationTokenSourceWithTimeout(
        TimeSpan timeoutDuration,
        Action onTimeout,
        Action onDispose,
        CancellationToken ct) : this(timeoutDuration, onTimeout, ct)
    {
        OnDispose = onDispose;
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
            OnTimeout?.Invoke();
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

            OnDispose?.Invoke();
        }
    }
}