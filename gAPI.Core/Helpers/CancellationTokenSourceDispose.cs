namespace gAPI.Core.Helpers;

public sealed class CancellationTokenSourceDispose : IDisposable
{
    private readonly CancellationTokenSource _cts;

    private bool IsDisposed;
    private readonly object Lock = new();
    private List<Action> OnDispose = [];
    private List<Func<Task>> OnDisposeAsync = [];

    public CancellationTokenSourceDispose(
        TimeSpan timeoutDuration)
        : this(null)
    {
        TimeoutDuration = timeoutDuration;
        _cts.CancelAfter(timeoutDuration);
    }
    public CancellationTokenSourceDispose(
        TimeSpan timeoutDuration,
        CancellationToken? ct = null)
        : this(ct)
    {
        TimeoutDuration = timeoutDuration;
        _cts.CancelAfter(timeoutDuration);
    }
    private CancellationTokenSourceDispose(
        CancellationToken? ct = null)
    {
        _cts = ct?.CanBeCanceled == true
            ? CancellationTokenSource.CreateLinkedTokenSource(ct.Value)
            : new CancellationTokenSource();
    }

    public CancellationToken Token => _cts.Token;
    public bool IsCancellationRequested => _cts.IsCancellationRequested;
    public TimeSpan TimeoutDuration { get; }
    public bool IsTimedOut => IsCancelled == false;
    private bool IsCancelled;


    public void Reset()
    {
        _cts.CancelAfter(TimeoutDuration);
    }
    public void Register(Func<Task> action)
    {
        _cts.Token.Register(async () =>
        {
            try
            {
                await action.Invoke();
            }
            catch
            {
            }
        });
    }
    public void Register(Action action)
    {
        _cts.Token.Register(() =>
        {
            try
            {
                action.Invoke();
            }
            catch
            {
            }
        });
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
    public void RegisterDispose(Func<Task> action)
    {
        if (IsDisposed)
            return;

        lock (Lock)
        {
            OnDisposeAsync.Add(action);
        }
    }

    public async Task CancelAsync()
    {
        if (IsDisposed)
            return;

        IsCancelled = true;

        await _cts.CancelAsync();
    }
    public void Cancel()
    {
        if (IsDisposed)
            return;

        IsCancelled = true;

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
            {
                try
                {
                    action.Invoke();
                }
                catch 
                {
                }
            }
            foreach (var action in OnDisposeAsync)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await action.Invoke();
                    }
                    catch
                    {
                    }
                });
            }
        }
    }

    public void ThrowIfCancellationRequested()
    {
        if (_cts == null)
            throw new ObjectDisposedException("Already disposed");

        _cts.Token.ThrowIfCancellationRequested();
    }
}