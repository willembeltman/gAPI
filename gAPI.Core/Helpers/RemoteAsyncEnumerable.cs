using System.Threading.Channels;

namespace gAPI.Core.Helpers;

public sealed class RemoteAsyncEnumerable<T> : IAsyncEnumerable<T>
{
    private readonly Channel<T> _items = Channel.CreateUnbounded<T>();
    private readonly Func<CancellationToken, Task> _requestNext;

    public RemoteAsyncEnumerable(Func<CancellationToken, Task> requestNext)
    {
        _requestNext = requestNext;
    }

    public void Push(T item)
    {
        if (!_items.Writer.TryWrite(item))
            throw new InvalidOperationException("The remote async enumerable is no longer accepting items.");
    }

    public void Complete(Exception? error = null)
    {
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
                return false;
            }
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}
