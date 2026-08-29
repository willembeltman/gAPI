using System.Threading.Channels;

namespace gAPI.Core.Helpers;

public sealed class RemoteAsyncEnumerable<T> : IAsyncEnumerable<T>
{
    private readonly Channel<T> _items = Channel.CreateUnbounded<T>();
    private readonly Func<CancellationToken, Task> _requestNext;
    private readonly ResettableTimeout _timeout;

    public RemoteAsyncEnumerable(Func<CancellationToken, Task> requestNext, TimeSpan? timeout = null)
    {
        _requestNext = requestNext;
        _timeout = new ResettableTimeout(
            timeout ?? TimeSpan.FromSeconds(60),
            () => Complete(new TimeoutException("Remote async enumerable timed out.")));
    }

    public void Push(T item)
    {
        if (!_items.Writer.TryWrite(item))
            throw new InvalidOperationException("The remote async enumerable is no longer accepting items.");

        _timeout.Reset();
    }

    public void Complete(Exception? error = null)
    {
        _timeout.Dispose();
        _items.Writer.TryComplete(error);
    }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new Enumerator(this, cancellationToken);
    }

    private sealed class Enumerator : IAsyncEnumerator<T>
    {
        private readonly RemoteAsyncEnumerable<T> _owner;
        private readonly CancellationToken _cancellationToken;

        public Enumerator(RemoteAsyncEnumerable<T> owner, CancellationToken cancellationToken)
        {
            _owner = owner;
            _cancellationToken = cancellationToken;
        }

        public T Current { get; private set; } = default!;

        public async ValueTask<bool> MoveNextAsync()
        {
            await _owner._requestNext(_cancellationToken);

            try
            {
                Current = await _owner._items.Reader.ReadAsync(_cancellationToken);
                return true;
            }
            catch (ChannelClosedException)
            {
                if (_owner._items.Reader.Completion.IsFaulted)
                    await _owner._items.Reader.Completion;

                return false;
            }
        }

        public ValueTask DisposeAsync()
        {
            _owner.Complete();
            return ValueTask.CompletedTask;
        }
    }
}
