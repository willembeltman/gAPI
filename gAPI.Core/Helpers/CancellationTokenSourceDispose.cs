namespace gAPI.Core.Helpers;

public sealed class CancellationTokenSourceDispose : IDisposable
{
    private readonly CancellationTokenSource _cts;

    private bool IsDisposed;
    private readonly object Lock = new();
    private List<Action> OnDispose = [];

    public CancellationTokenSourceDispose(
        TimeSpan timeoutDuration)
        : this(null)
    {
        _cts.CancelAfter(timeoutDuration);
    }
    public CancellationTokenSourceDispose(
        TimeSpan timeoutDuration,
        CancellationToken? ct = null)
        : this(ct)
    {
        _cts.CancelAfter(timeoutDuration);
    }
    public CancellationTokenSourceDispose(
        CancellationToken? ct = null)
    {
        _cts = ct?.CanBeCanceled == true
            ? CancellationTokenSource.CreateLinkedTokenSource(ct.Value)
            : new CancellationTokenSource();
    }

    public CancellationToken Token => _cts.Token;
    public bool IsCancellationRequested => _cts.IsCancellationRequested;

    public void Register(Action action)
    {
        _cts.Token.Register(action);
    }
    public void RegisterDispose(Action action)
    {
        if (IsDisposed)
            return;

        lock (Lock)
        {
            OnDispose.Add(action);
        }
    }

    public async Task CancelAsync()
    {
        if (IsDisposed)
            return;

        await _cts.CancelAsync();
    }
    public void Cancel()
    {
        if (IsDisposed)
            return;

        _cts.Cancel();
    }
    public void CancelAfter(TimeSpan timeSpan)
    {
        if (IsDisposed)
            return;

        _cts.CancelAfter(timeSpan);
    }

    public void Dispose()
    {
        if (IsDisposed)
            return;

        IsDisposed = true;

        _cts.Dispose();

        lock (Lock)
        {
            foreach (var action in OnDispose)
                action.Invoke();
        }
    }
}