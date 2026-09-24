namespace gAPI.Core.Helpers;

public sealed class ResettableTimeout : IDisposable
{
    private readonly TimeSpan _timeout;
    private readonly Action _onTimeout;
    private readonly object _lock = new();

    private CancellationTokenSource? _cts;
    private CancellationTokenRegistration _registration;
    private bool _disposed;

    public ResettableTimeout(TimeSpan timeoutDuration, Action onTimeout)
    {
        _timeout = timeoutDuration;
        _onTimeout = onTimeout;

        lock (_lock)
        {
            _cts = new CancellationTokenSource(_timeout);
            // Registreer de OnTimeout actie die direct afgaat als de CTS afloopt
            _registration = _cts.Token.Register(ExecuteTimeout);
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            if (_disposed || _cts == null)
                return;

            // Reset de timer DIRECT vanaf NU naar de volledige duur
            _cts.CancelAfter(_timeout);
        }
    }

    private void ExecuteTimeout()
    {
        lock (_lock)
        {
            if (_disposed)
                return;

            _onTimeout();
            DisposeInternal();
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            DisposeInternal();
        }
    }

    private void DisposeInternal()
    {
        if (_disposed)
            return;

        _disposed = true;
        _registration.Dispose(); // Altijd de registratie netjes opruimen
        _cts?.Dispose();
        _cts = null;
    }
}



//namespace gAPI.Core.Helpers;

//public sealed class ResettableTimeout : IDisposable
//{
//    private readonly TimeSpan _timeout;
//    private readonly Action _onTimeout;

//    private readonly object _lock = new();
//    private CancellationTokenSource _cts;
//    private bool _disposed;

//    public bool ResetFlag { get; private set; }

//    public ResettableTimeout(TimeSpan timeoutDuration, Action onTimeout)
//    {
//        _timeout = timeoutDuration;
//        _onTimeout = onTimeout;

//        _cts = new CancellationTokenSource();
//        _ = RunAsync(_cts.Token);
//    }

//    public void Reset()
//    {
//        lock (_lock)
//        {
//            if (_disposed)
//                return;

//            ResetFlag = true;
//        }
//    }

//    private async Task RunAsync(CancellationToken ct)
//    {
//        try
//        {
//            ResetFlag = true;
//            while (ResetFlag)
//            {
//                ResetFlag = false;
//                await Task.Delay(_timeout, ct);
//            }
//            if (!_disposed && !ct.IsCancellationRequested)
//            {
//                _onTimeout();
//                Dispose();
//            }
//        }
//        catch (OperationCanceledException)
//        {
//            Dispose();
//        }
//    }

//    public void Dispose()
//    {
//        lock (_lock)
//        {
//            if (_disposed)
//                return;

//            _disposed = true;

//            //_cts.Cancel();
//            _cts.Dispose();
//        }
//    }
//}